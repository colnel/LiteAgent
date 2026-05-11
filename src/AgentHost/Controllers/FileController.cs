using System.Text.Json;

namespace LiteAgent.AgentHost.Controllers;
/// <summary>
/// 文件上传返回信息
/// </summary>
public class UploadedFile
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public long Bytes { get; set; }
    public long CreatedAt { get; set; }
    public string Filename { get; set; } = "";
    public string Purpose { get; set; } = "";
}
public class FileController
{

}
