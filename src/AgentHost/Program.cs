using LiteAgent.AgentHost.Models;
using LiteAgent.AgentHost.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DataSetting>(builder.Configuration.GetSection("DataSettings"));
builder.Services.Configure<LlmSetting>(builder.Configuration.GetSection("LlmSetting"));

builder.Services.AddScoped<DataService>();
builder.Services.AddSingleton<LlmClient>();

//builder.Logging.ClearProviders();
//builder.Logging.AddTxtLogger();

builder.Services.AddCors(options =>
{
    options.AddPolicy("any", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBufferSize = 268435456; });
builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 268435456; });
var app = builder.Build();

app.MapGet("/", () => "            !");
//app.UseHttpsRedirection();


app.UseRouting();
app.UseCors("any");
app.MapControllers();
app.Run();
