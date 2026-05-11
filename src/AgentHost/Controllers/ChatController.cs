using LiteAgent.AgentHost.Models;
using LiteAgent.AgentHost.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LiteAgent.AgentHost.Controllers;


[ApiController]
[Route("[controller]")]
public class ChatController(LlmClient _llmClient, ILogger<ChatController> _logger) : ControllerBase
{

    /// <summary>
    /// 非流式调用：发送单条用户消息（可附带系统提示），返回完整回复
    /// </summary>
    [HttpPost("text")]
    public async Task<IActionResult> TextCompletion([FromBody] List<RequestMessage> messages)
    {
        try
        {
            var response = await _llmClient.ChatAsync(
                messages: messages
            );
            return Ok(new
            {
                content = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "非流式调用失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 流式调用（Server-Sent Events）：单条用户消息
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamCompletion([FromBody] List<RequestMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            await _llmClient.ChatStreamAsync(messages, async token =>
            {
                var data = $"data: {JsonSerializer.Serialize(new { token })}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await Response.Body.WriteAsync(bytes, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }, cancellationToken);

            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("data: [DONE]\n\n"), cancellationToken);
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

}
