
using AgentHost;
using AgentHost.Models;
using AgentHost.Services;
using LiteAgent.AgentHost.Models;
using LiteAgent.AgentHost.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using VideoLink.Utilities;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DataSetting>(builder.Configuration.GetSection("DataSettings"));
builder.Services.Configure<DomainSettings>(builder.Configuration.GetSection("DomainSettings"));

builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<DbService>();
builder.Services.AddSingleton<StreamService>();
builder.Services.AddSingleton<SipService>();
builder.Services.AddSingleton<TestService>();

//builder.Logging.ClearProviders();
builder.Logging.AddTxtLogger();

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

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


app.UseRouting();
app.UseCors("any");
app.MapControllers();
app.Run();



app.Run();

/*
  string apiKey = "your-deepseek-api-key";
        using var client = new LlmClient(apiKey);

        // 1. 简单非流式调用
        var response = await client.SendMessageAsync(
            systemPrompt: "你是一个有用的助手",
            userMessage: "请介绍一下 C# 的新特性"
        );

        Console.WriteLine("【非流式响应】");
        Console.WriteLine(response.Choices[0].Message.Content);
        Console.WriteLine($"Token 用量: 输入 {response.Usage?.PromptTokens}, 输出 {response.Usage?.CompletionTokens}\n");

        // 2. 多轮对话非流式
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content = "你是一位编程专家" },
            new ChatMessage { Role = "user", Content = "什么是 async/await？" },
            new ChatMessage { Role = "assistant", Content = "async/await 是 C# 中用于异步编程的关键字..." }, // 可保留历史
            new ChatMessage { Role = "user", Content = "给出一个简单的例子" }
        };
        var multiResponse = await client.SendMessageAsync(messages, temperature: 0.7, maxTokens: 500);
        Console.WriteLine("【多轮对话响应】");
        Console.WriteLine(multiResponse.Choices[0].Message.Content);

        // 3. 流式输出（回调方式）
        Console.WriteLine("\n【流式输出（回调）】");
        await client.SendMessageStreamAsync(
            messages: new List<ChatMessage> { new ChatMessage { Role = "user", Content = "写一首关于编程的短诗" } },
            onDelta: delta => Console.Write(delta)
        );
        Console.WriteLine("\n");

        // 4. 流式输出（IAsyncEnumerable 方式）
        Console.WriteLine("【流式输出（IAsyncEnumerable）】");
        await foreach (var chunk in client.SendMessageStreamAsync(
            messages: new List<ChatMessage> { new ChatMessage { Role = "user", Content = "用中文解释什么是大语言模型" } }))
        {
            Console.Write(chunk);
        }
        Console.WriteLine();
*/