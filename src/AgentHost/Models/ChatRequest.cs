using System.Text;

namespace LiteAgent.AgentHost.Models;

/// <summary>
/// 消息角色
/// </summary>
public static class Role
{
    public const string System = "system";
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string Tool = "tool";
}
/// <summary>
/// 请求消息
/// </summary>
public class RequestMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ToolCallId { get; set; }
}

/// <summary>
/// 聊天请求体
/// </summary>
public class ChatRequest
{
    public List<RequestMessage> Messages { get; set; } = [];
    public string Model { get; set; } = "deepseek-v4-pro";
    public object Thinking { get; set; } = new { type = "enabled" };  // 思考模式，否则为默认的 "disabled"
    public bool Stream { get; set; } = true;
    public List<Tool>? Tools { get; set; }
    public string? ToolChoice { get; set; } // "auto", "none", 或指定工具
}

/// <summary>
/// 工具定义
/// </summary>
public class Tool
{
    public string Type { get; set; } = "function";
    public FunctionDefinition Function { get; set; } = new();
}

public class FunctionDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object? Parameters { get; set; }  // JSON Schema
}

