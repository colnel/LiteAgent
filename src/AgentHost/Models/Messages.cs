
using System.ComponentModel.DataAnnotations;

namespace LiteAgent.AgentHost.Models;


/// <summary>
/// 聊天请求模型（非流式）
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// 系统提示词（可选）
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// 用户消息
    /// </summary>
    [Required]
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称，默认 deepseek-chat
    /// </summary>
    public string Model { get; set; } = "deepseek-chat";

    /// <summary>
    /// 温度参数 0~2
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// 最大输出 token 数
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 核采样参数
    /// </summary>
    public double? TopP { get; set; }
}

/// <summary>
/// 多轮对话消息（用于流式或非流式的多轮对话）
/// </summary>
public class ConversationMessage
{
    [Required]
    public string Role { get; set; } = string.Empty;  // "user", "assistant", "system"
    [Required]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 多轮对话请求
/// </summary>
public class ConversationRequest
{
    [Required]
    public List<ConversationMessage> Messages { get; set; } = [];
    public string Model { get; set; } = "deepseek-chat";
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public double? TopP { get; set; }
}

/// <summary>
/// 非流式响应
/// </summary>
public class ChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}
