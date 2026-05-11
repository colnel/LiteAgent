namespace LiteAgent.AgentHost.Models;

public class FileRequest
{
}

/// <summary>
/// 多模态消息中的内容片段
/// </summary>
public class ContentPart
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
    public ImageUrl? ImageUrl { get; set; }
}

public class ImageUrl
{
    public string Url { get; set; } = "";
}
