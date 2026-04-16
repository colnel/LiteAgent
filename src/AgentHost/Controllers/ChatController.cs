using LiteAgent.AgentHost.Models;
using LiteAgent.AgentHost.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiteAgent.AgentHost.Controllers;


[ApiController]
[Route("[controller]")]
public class ChatController(DataService _dataService, LlmClient _llmClient, ILogger<ChatController> _logger) : ControllerBase
{

    /// <summary>
    /// 非流式调用：发送单条用户消息（可附带系统提示），返回完整回复
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _llmClient.SendMessageAsync(
                systemPrompt: request.SystemPrompt,
                userMessage: request.UserMessage,
                model: request.Model,
                temperature: request.Temperature,
                maxTokens: request.MaxTokens,
                topP: request.TopP
            );

            var result = new ChatResponse
            {
                Id = response.Id,
                Model = response.Model,
                Content = response.Choices.Count > 0 ? response.Choices[0].Message.Content : string.Empty,
                PromptTokens = response.Usage?.PromptTokens,
                CompletionTokens = response.Usage?.CompletionTokens,
                TotalTokens = response.Usage?.TotalTokens
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "非流式调用失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 非流式调用：多轮对话
    /// </summary>
    [HttpPost("converse")]
    public async Task<ActionResult<ChatResponse>> Converse([FromBody] ConversationRequest request)
    {
        try
        {
            var messages = request.Messages.ConvertAll(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            });

            var response = await _llmClient.SendMessageAsync(
                messages: messages,
                model: request.Model,
                temperature: request.Temperature,
                maxTokens: request.MaxTokens,
                topP: request.TopP
            );

            var result = new ChatResponse
            {
                Id = response.Id,
                Model = response.Model,
                Content = response.Choices.Count > 0 ? response.Choices[0].Message.Content : string.Empty,
                PromptTokens = response.Usage?.PromptTokens,
                CompletionTokens = response.Usage?.CompletionTokens,
                TotalTokens = response.Usage?.TotalTokens
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多轮对话失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 流式调用（Server-Sent Events）：单条用户消息
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessage([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new ChatMessage { Role = "system", Content = request.SystemPrompt });
        messages.Add(new ChatMessage { Role = "user", Content = request.UserMessage });

        try
        {
            await foreach (var chunk in _llmClient.SendMessageStreamAsync(
                messages: messages,
                model: request.Model,
                temperature: request.Temperature,
                maxTokens: request.MaxTokens,
                topP: request.TopP,
                cancellationToken: cancellationToken))
            {
                // 按 SSE 格式发送数据块
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // 发送结束标记
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 客户端断开连接，正常结束
            _logger.LogInformation("流式请求被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流式调用失败");
            await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 流式调用（Server-Sent Events）：多轮对话
    /// </summary>
    [HttpPost("stream-converse")]
    public async Task StreamConverse([FromBody] ConversationRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var messages = request.Messages.ConvertAll(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content
        });

        try
        {
            await foreach (var chunk in _llmClient.SendMessageStreamAsync(
                messages: messages,
                model: request.Model,
                temperature: request.Temperature,
                maxTokens: request.MaxTokens,
                topP: request.TopP,
                cancellationToken: cancellationToken))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("流式多轮对话被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流式多轮对话失败");
            await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }


}



/*
  string apiKey = "your-deepseek-api-key";
        using var client = new LlmClient(apiKey);

        // 1. 简单非流式调用
        var response = await client.SendMessageAsync(
            systemPrompt: "你是一个有用的助手",
            userMessage: "请介绍一下 C# 的新特性"
        );

        Console.WriteLine("【非流式响应】");
        Console.WriteLine(response.Choices[0].Message.Content);
        Console.WriteLine($"Token 用量: 输入 {response.Usage?.PromptTokens}, 输出 {response.Usage?.CompletionTokens}\n");

        // 2. 多轮对话非流式
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content = "你是一位编程专家" },
            new ChatMessage { Role = "user", Content = "什么是 async/await？" },
            new ChatMessage { Role = "assistant", Content = "async/await 是 C# 中用于异步编程的关键字..." }, // 可保留历史
            new ChatMessage { Role = "user", Content = "给出一个简单的例子" }
        };
        var multiResponse = await client.SendMessageAsync(messages, temperature: 0.7, maxTokens: 500);
        Console.WriteLine("【多轮对话响应】");
        Console.WriteLine(multiResponse.Choices[0].Message.Content);

        // 3. 流式输出（回调方式）
        Console.WriteLine("\n【流式输出（回调）】");
        await client.SendMessageStreamAsync(
            messages: new List<ChatMessage> { new ChatMessage { Role = "user", Content = "写一首关于编程的短诗" } },
            onDelta: delta => Console.Write(delta)
        );
        Console.WriteLine("\n");

        // 4. 流式输出（IAsyncEnumerable 方式）
        Console.WriteLine("【流式输出（IAsyncEnumerable）】");
        await foreach (var chunk in client.SendMessageStreamAsync(
            messages: new List<ChatMessage> { new ChatMessage { Role = "user", Content = "用中文解释什么是大语言模型" } }))
        {
            Console.Write(chunk);
        }
        Console.WriteLine();
*/