namespace LiteAgent.AgentHost.Models;

public class DataSetting()
{
    public string ConnectionString { get; set; } = "";

    public string CityId { get; set; } = null!;


}

public class LlmSetting()
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 120;
}