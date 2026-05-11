using System.Text;

namespace LiteAgent.AgentHost.Models;


/// <summary>
/// 流式响应块
/// </summary>
internal class StreamChunk
{
    public string Id { get; set; } = "";
    public List<ChoiceDelta> Choices { get; set; } = [];
}

internal class ChoiceDelta
{
    public int Index { get; set; }
    public DeltaMessage Delta { get; set; } = new DeltaMessage();
    public string? FinishReason { get; set; }
}

internal class DeltaMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
    public List<ToolCallDelta>? ToolCalls { get; set; }
}


internal class ToolCallDelta
{
    public int Index { get; set; }
    public string? Id { get; set; }
    public string? Type { get; set; }
    public FunctionCallDelta? Function { get; set; }
}

internal class FunctionCallDelta
{
    public string? Name { get; set; }
    public string? Arguments { get; set; }
}

/// <summary>
/// 用于收集流式响应的辅助类
/// </summary>
internal class StreamingResponseBuilder
{
    private readonly StringBuilder _contentBuilder = new();
    private readonly Dictionary<int, ToolCallBuilder> _toolCallBuilders = new();

    public string Content => _contentBuilder.ToString();
    public List<ToolCall>? ToolCalls => _toolCallBuilders.Count == 0 ? null : BuildToolCalls();

    public void AppendContent(string text)
    {
        _contentBuilder.Append(text);
    }

    public void AppendToolCallDelta(ToolCallDelta delta)
    {
        if (!_toolCallBuilders.TryGetValue(delta.Index, out var builder))
        {
            builder = new ToolCallBuilder();
            _toolCallBuilders[delta.Index] = builder;
        }

        if (!string.IsNullOrEmpty(delta.Id))
            builder.Id = delta.Id;
        if (!string.IsNullOrEmpty(delta.Type))
            builder.Type = delta.Type;
        if (delta.Function != null)
        {
            if (!string.IsNullOrEmpty(delta.Function.Name))
                builder.FunctionName = delta.Function.Name;
            if (!string.IsNullOrEmpty(delta.Function.Arguments))
                builder.ArgumentsBuilder.Append(delta.Function.Arguments);
        }
    }

    private List<ToolCall> BuildToolCalls()
    {
        var result = new List<ToolCall>();
        foreach (var builder in _toolCallBuilders.Values)
        {
            result.Add(new ToolCall
            {
                Id = builder.Id,
                Type = builder.Type,
                Function = new FunctionCall
                {
                    Name = builder.FunctionName,
                    Arguments = builder.ArgumentsBuilder.ToString()
                }
            });
        }
        return result;
    }

    private class ToolCallBuilder
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "function";
        public string FunctionName { get; set; } = "";
        public StringBuilder ArgumentsBuilder { get; } = new();
    }
}
