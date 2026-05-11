using LiteAgent.AgentHost.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LiteAgent.AgentHost.Services;

/// <summary>
/// DeepSeek 大模型 API 调用客户端
/// </summary>
public class LlmClient(IOptions<LlmSetting> _setting, ILogger<LlmClient> _logger) : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string _baseUrl = _setting.Value.BaseUrl.TrimEnd('/');
    private readonly string _chatModel = _setting.Value.ChatModel;
    private readonly string _reasoningModel = _setting.Value.ReasoningModel;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };


    /// <summary>
    /// Performs initialization logic for the current instance.
    /// </summary>
    internal void Initialize()
    {
        try
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_setting.Value.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _setting.Value.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Service} 初始化失败: {ChatMessage}", nameof(LlmClient), ex.Message);
        }
    }

    /// <summary>
    /// 非流式对话，返回完整的回复文本
    /// </summary>
    /// <param name="messages">对话历史</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型生成的完整回答</returns>
    public async Task<string> ChatAsync(List<RequestMessage> messages, CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Messages = messages,
            Stream = false,
            Model = _chatModel,
            Thinking = new
            {
                type = "disable"
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseJson, _jsonOptions);

        return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }
    /// <summary>
    /// 流式对话，实时输出每个 token
    /// </summary>
    /// <param name="messages">对话历史</param>
    /// <param name="onToken">接收到文本 token 时的回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ChatStreamAsync(List<RequestMessage> messages, Action<string> onToken, CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Messages = messages,
            Model = _reasoningModel
        };
        await StreamChatCompletionsAsync(request, onToken, null, cancellationToken);
    }

    public async Task<ChatResponse> ToolCallAsync(List<RequestMessage> messages, List<Tool> tools, CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Messages = messages,
            Model = _reasoningModel,
            Tools = tools,
        };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseJson, _jsonOptions);
        return chatResponse ?? new ChatResponse();
    }

    /// <summary>
    /// 技能调用（Agent Loop），支持流式输出文本增量，自动处理函数调用循环
    /// </summary>
    /// <param name="messages">对话历史</param>
    /// <param name="tools">可用工具列表</param>
    /// <param name="functionExecutor">执行函数的委托，参数为 (functionName, argumentsJson)，返回函数的执行结果（字符串）</param>
    /// <param name="onTextToken">接收到文本 token 时的回调（流式）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ToolUseAsync(
        List<RequestMessage> messages,
        List<Tool> tools,
        Func<string, string, Task<string>> functionExecutor,
        Action<string> onTextToken,
        CancellationToken cancellationToken = default)
    {
        // 深拷贝消息列表，避免修改外部数据
        var currentMessages = new List<RequestMessage>(messages);
        while (true)
        {
            var request = new ChatRequest
            {
                Messages = currentMessages,
                Model = _reasoningModel,
                Tools = tools,
            };

            // 收集本次请求的最终内容（无工具调用时）或工具调用列表
            string? assistantContent = null;
            List<ToolCall>? toolCalls = null;

            await StreamChatCompletionsAsync(request, onTextToken, (builder) =>
            {
                // 流结束后从 builder 中提取结果
                assistantContent = builder.Content;
                toolCalls = builder.ToolCalls;
            }, cancellationToken);

            // 如果模型没有请求工具调用，则结束循环
            if (toolCalls == null || toolCalls.Count == 0)
            {
                if (!string.IsNullOrEmpty(assistantContent))
                {
                    // 最终的助手消息已经通过 onTextToken 流式输出过了，无需再次输出
                }
                break;
            }

            // 将本次assistant消息加入历史（即使没有内容也要加，保持状态）
            currentMessages.Add(new RequestMessage()
            {
                Role = Role.Assistant,
                Content = assistantContent ?? ""
            });

            // 执行每个工具调用，并将结果作为 Tool 消息返回
            foreach (var toolCall in toolCalls)
            {
                string result = await functionExecutor(toolCall.Function.Name, toolCall.Function.Arguments);
                currentMessages.Add(new RequestMessage()
                {
                    Role = Role.Tool,
                    ToolCallId = toolCall.Id,
                    Content = result
                });
            }
        }
    }

    /// <summary>
    /// 核心流式请求处理，支持工具调用增量收集和文本实时输出
    /// </summary>
    private async Task StreamChatCompletionsAsync(ChatRequest request, Action<string> onTextToken, Action<StreamingResponseBuilder>? onComplete, CancellationToken cancellationToken)
    {
        request.Stream = true;
        var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Accept", "text/event-stream");

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var builder = new StreamingResponseBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);
            if (data == "[DONE]")
                break;

            try
            {
                var chunk = JsonSerializer.Deserialize<StreamChunk>(data, _jsonOptions);
                if (chunk?.Choices == null || chunk.Choices.Count == 0)
                    continue;

                var delta = chunk.Choices[0].Delta;
                if (delta == null)
                    continue;

                // 处理文本 token
                if (!string.IsNullOrEmpty(delta.Content))
                {
                    onTextToken(delta.Content);
                    builder.AppendContent(delta.Content);
                }

                // 处理工具调用增量
                if (delta.ToolCalls != null)
                {
                    foreach (var toolDelta in delta.ToolCalls)
                    {
                        builder.AppendToolCallDelta(toolDelta);
                    }
                }
            }
            catch (JsonException)
            {
                // 忽略解析错误，继续处理
            }
        }

        onComplete?.Invoke(builder);
    }

    /// <summary>
    /// 释放 HttpClient 资源
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}