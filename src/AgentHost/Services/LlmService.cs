using LiteAgent.AgentHost.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LiteAgent.AgentHost.Services;

/// <summary>
/// 表示一个对话消息（角色 + 内容）
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = string.Empty;   // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 非流式响应中的选择项
/// </summary>
public class Choice
{
    public int Index { get; set; }
    public ChatMessage Message { get; set; } = new();
    public string? FinishReason { get; set; }
}

/// <summary>
/// 用量统计
/// </summary>
public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>
/// DeepSeek API 非流式响应的整体结构
/// </summary>
public class ChatCompletionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<Choice> Choices { get; set; } = new();
    public Usage? Usage { get; set; }
}

/// <summary>
/// DeepSeek API 请求参数
/// </summary>
public class ChatCompletionRequest
{
    public string Model { get; set; } = "deepseek-chat";
    public List<ChatMessage> Messages { get; set; } = new();
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public double? TopP { get; set; }
    public bool? Stream { get; set; }
    // 可根据需要添加更多参数
}


/// <summary>
/// DeepSeek 大模型 API 调用客户端
/// </summary>
public class LlmClient(IOptions<LlmSetting> _setting, ILogger<LlmClient> _logger) : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string _baseUrl = _setting.Value.BaseUrl.TrimEnd('/');

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Performs initialization logic for the current instance.
    /// </summary>
    internal void Initialize()
    {
        try
        {
#pragma warning disable CA1873 // 避免进行可能成本高昂的日志记录
            _logger.LogInformation("{Service} 初始化中...", nameof(LlmClient));
#pragma warning restore CA1873 // 避免进行可能成本高昂的日志记录
            _httpClient.Timeout = TimeSpan.FromSeconds(_setting.Value.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _setting.Value.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Service} 初始化失败: {Message}", nameof(LlmClient), ex.Message);
        }
    }

    /// <summary>
    /// 发送聊天消息（非流式），返回完整响应对象
    /// </summary>
    /// <param name="messages">对话消息列表</param>
    /// <param name="model">模型名称，默认 deepseek-chat</param>
    /// <param name="temperature">温度参数 (0~2)</param>
    /// <param name="maxTokens">最大输出 token 数</param>
    /// <param name="topP">核采样参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API 返回的响应对象</returns>
    public async Task<ChatCompletionResponse> SendMessageAsync(
        List<ChatMessage> messages,
        string model = "deepseek-chat",
        double? temperature = null,
        int? maxTokens = null,
        double? topP = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = temperature,
            MaxTokens = maxTokens,
            TopP = topP,
            Stream = false
        };

        return await SendRequestAsync(request, cancellationToken);
    }

    /// <summary>
    /// 发送聊天消息（非流式），使用简单的 system 和 user 消息
    /// </summary>
    /// <param name="systemPrompt">系统提示（可选）</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="model">模型名称</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="maxTokens">最大输出 token 数</param>
    /// <param name="topP">核采样参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API 返回的响应对象</returns>
    public async Task<ChatCompletionResponse> SendMessageAsync(
        string? systemPrompt,
        string userMessage,
        string model = "deepseek-chat",
        double? temperature = null,
        int? maxTokens = null,
        double? topP = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });
        messages.Add(new ChatMessage { Role = "user", Content = userMessage });

        return await SendMessageAsync(messages, model, temperature, maxTokens, topP, cancellationToken);
    }

    /// <summary>
    /// 发送流式请求，逐块返回增量内容
    /// </summary>
    /// <param name="messages">对话消息列表</param>
    /// <param name="onDelta">每个数据块的回调，参数为增量文本</param>
    /// <param name="model">模型名称</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="maxTokens">最大输出 token 数</param>
    /// <param name="topP">核采样参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SendMessageStreamAsync(
        List<ChatMessage> messages,
        Action<string> onDelta,
        string model = "deepseek-chat",
        double? temperature = null,
        int? maxTokens = null,
        double? topP = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = temperature,
            MaxTokens = maxTokens,
            TopP = topP,
            Stream = true
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break; // 流结束
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentProp))
                        {
                            var deltaContent = contentProp.GetString();
                            if (!string.IsNullOrEmpty(deltaContent))
                                onDelta(deltaContent);
                        }
                    }
                }
                catch (JsonException)
                {
                    // 忽略解析错误，继续处理下一块
                }
            }
        }
    }

    /// <summary>
    /// 发送流式请求，使用 IAsyncEnumerable 返回增量块
    /// </summary>
    /// <param name="messages">对话消息列表</param>
    /// <param name="model">模型名称</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="maxTokens">最大输出 token 数</param>
    /// <param name="topP">核采样参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步枚举增量文本</returns>
    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        List<ChatMessage> messages,
        string model = "deepseek-chat",
        double? temperature = null,
        int? maxTokens = null,
        double? topP = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ChatCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = temperature,
            MaxTokens = maxTokens,
            TopP = topP,
            Stream = true
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break; // 流结束
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                string? deltaContent = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentProp))
                        {
                            deltaContent = contentProp.GetString();
                        }
                    }
                }
                catch (JsonException)
                {
                    // 忽略解析错误
                }
                if (!string.IsNullOrEmpty(deltaContent))
                    yield return deltaContent;
            }
        }
    }

    private async Task<ChatCompletionResponse> SendRequestAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API 请求失败 ({response.StatusCode}): {responseJson}");
        }

        var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, _jsonOptions);
        if (result == null)
            throw new InvalidOperationException("无法解析 API 响应");

        return result;
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