namespace LiteAgent.AgentHost.Models;

public class DataSetting()
{
    public string ConnectionString { get; set; } = "";

    public string CityId { get; set; } = null!;


}

public class DomainSettings()
{
    public int RegisterInterval { get; set; } = 60;
    public int AliveInterval { get; set; } = 30;
    public int AliveLostCount { get; set; } = 3;
}