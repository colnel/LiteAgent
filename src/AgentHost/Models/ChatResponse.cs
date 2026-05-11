
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LiteAgent.AgentHost.Models;


/// <summary>
/// 非流式完整响应
/// </summary>
public class ChatResponse
{
    public string Id { get; set; } = "";
    public List<Choice> Choices { get; set; } = [];
    public double Created { get; set; }
    public string Model { get; set; } = "";
    public string? Object { get; set; } 
    public Usage? Usage { get; set; }
}
/// <summary>
/// 回复消息
/// </summary>
public class ResponseMessage
{
    public string Role { get; set; } = "";
    public string? Content { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
}

/// <summary>
/// 工具调用（请求）
/// </summary>
public class ToolCall
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "function";
    public FunctionCall Function { get; set; } = new FunctionCall();
}

public class FunctionCall
{
    public string Name { get; set; } = "";
    public string Arguments { get; set; } = ""; // JSON字符串
}

public class Choice
{
    public int Index { get; set; }
    public ResponseMessage Message { get; set; } = new ();
    public string FinishReason { get; set; } = "";
}
public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
