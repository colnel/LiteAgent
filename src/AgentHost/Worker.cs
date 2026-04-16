using LiteAgent.AgentHost.Models;
using LiteAgent.AgentHost.Services;
using Microsoft.Extensions.Options;
using System.Text;

namespace LiteAgent.AgentHost;

public sealed class Worker(IServiceProvider serviceProvider, ILogger<Worker> _logger)
    : IHostedService, IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly Task _completedTask = Task.CompletedTask;

    private LlmClient? _llmClient;
    private Timer? _timer;

    /// <summary>
    /// 启动各项服务
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("{Service} 开始启动...", nameof(Worker));

            //注册Nuget包System.Text.Encoding.CodePages中的编码到.NET Core，用于支持GB2312等编码方式
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _llmClient = _serviceProvider.GetService<LlmClient>();
            _llmClient?.Initialize();

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            // TestAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker 启动失败:{ex}", ex);
        }

        return _completedTask;
    }

    //测试服务
    async void TestAsync()
    {

        //   TestService? test = _serviceProvider.GetService<TestService>();
        //   test!.Authenticate();
        //  test!.Invite();
        //   await test!.RegisterUserAgent();
        //  test!.LoadCatalog();
    }
    private void DoWork(object? state)
    {
        try
        {
          //  _llmClient?.TickWork();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时任务出现异常:{ex}", ex);
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Service} 正在停止.", nameof(Worker));

        _timer?.Change(Timeout.Infinite, 0);

        return _completedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
        {
            await timer.DisposeAsync();
        }
        _timer = null;
        GC.SuppressFinalize(this);
    }
}