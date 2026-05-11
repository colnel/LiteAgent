using LiteAgent.AgentHost.Models;
using LiteAgent.AgentHost.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace LiteAgent.AgentHost.Controllers;

[ApiController]
[Route("[controller]")]
public class SkillController(LlmClient _llmClient,DataService dataService, ILogger<SkillController> _logger) : ControllerBase
{

    [HttpGet("list/isactive={isActive}")]
    public async Task<IActionResult> GetSkills(string isActive)
    {
        try
        {
            string selectSql = $"SELECT Id, Name, Description FROM agent_skills where IsActive = {isActive}";
            var records = await dataService.ExecuteReaderAsync(selectSql);
            return Ok(records);
        }
        catch (Exception ex) { 
            _logger.LogError(ex, "获取技能列表失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("profile/id={skillId}")]
    public async Task<IActionResult> GetSkillProfile(string skillId)
    {
        try
        {
            string sql = $"select * from agent_skills where Id=\"{skillId}\"";
            var records = await dataService.ExecuteReaderAsync(sql);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取技能信息失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddSkill([FromBody] AgentSkill skill)
    {
        try
        {
            var newId = await dataService.AddSkill(skill);
            if (newId > 0)
                return Ok(new { id = newId });
            else
                return StatusCode(500, new { error = "添加技能失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加技能失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateSkill([FromBody] AgentSkill skill)
    {
        try
        {
            var success = await dataService.UpdateSkill(skill);
            if (success>0)
                return Ok(new { message = "技能更新成功" });
            else
                return StatusCode(500, new { error = "技能更新失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新技能失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("delete/id={skillId}")]
    public async Task<IActionResult> DeleteSkill(int skillId)
    {
        try
        {
            var success = await dataService.DeleteSkill(skillId);
            if (success > 0)
                return Ok(new { message = "技能删除成功" });
            else
                return StatusCode(500, new { error = "技能删除失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除技能失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 选择技能接口，接收用户消息和可选工具列表，返回模型选择的技能（工具调用）结果
    /// </summary>
    [HttpPost("call")]
    public async Task<IActionResult> ToolCall([FromBody] List<RequestMessage> messages, [FromBody] List<Tool> tools)
    {
        try
        {
            var response = await _llmClient.ToolCallAsync(
                messages: messages,
                tools: tools
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "非流式调用失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 执行技能，并通过SSE流式返回结果，适用于需要多轮交互或长时间执行的技能
    /// </summary>
    [HttpPost("execute")]
    public async Task SkillExecute([FromBody] List<RequestMessage> messages,
            List<Tool> tools, CancellationToken cancellationToken)
    {
        try
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            Func<string,string,Task<string>> toolExecutor = async (toolName, argsJson) =>
            {
                using var doc = JsonDocument.Parse(argsJson);
                if (toolName == "get_weather")
                {
                    var city = doc.RootElement.GetProperty("city").GetString();
                    await Task.Delay(100); // 模拟网络延迟
                    return $"{city}的天气是晴天，25°C。";
                }
                else if (toolName == "calculate")
                {
                    var expr = doc.RootElement.GetProperty("expression").GetString();
                    try
                    {
                        var result = new System.Data.DataTable().Compute(expr, null);
                        return result.ToString() ?? "计算错误";
                    }
                    catch
                    {
                        return "表达式无效";
                    }
                }
                return "未知工具";
            };

            await _llmClient.ToolUseAsync(
                     messages,
                     tools,
                     toolExecutor,
                     async (token) =>
                    {
                        /*
                          var data = $"data: {JsonSerializer.Serialize(new { token })}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await Response.Body.WriteAsync(bytes, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                         */
                        await Response.WriteAsync($"data: {token}\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    },
                     cancellationToken);
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

    // 工具执行逻辑
    async Task<string> ExecuteTool(string toolName, string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        if (toolName == "get_weather")
        {
            var city = doc.RootElement.GetProperty("city").GetString();
            await Task.Delay(100); // 模拟网络延迟
            return $"{city}的天气是晴天，25°C。";
        }
        else if (toolName == "calculate")
        {
            var expr = doc.RootElement.GetProperty("expression").GetString();
            try
            {
                var result = new System.Data.DataTable().Compute(expr, null);
                return result.ToString() ?? "计算错误";
            }
            catch
            {
                return "表达式无效";
            }
        }
        return "未知工具";
    }

}
