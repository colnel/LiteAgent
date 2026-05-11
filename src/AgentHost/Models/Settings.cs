namespace LiteAgent.AgentHost.Models;

public class DataSetting()
{
    public string ConnectionString { get; set; } = "";

    public int Timeout { get; set; } = 5000;

}
public class LlmSetting()
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ChatModel { get; set; } = "deepseek-v4-flash";
    public string ReasoningModel { get; set; } = "deepseek-v4-pro";
    public int TimeoutSeconds { get; set; } = 120;
}