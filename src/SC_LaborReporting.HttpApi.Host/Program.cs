using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SC_LaborReporting.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SC_LaborReporting;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting SC_LaborReporting.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);

            // 获取配置中的连接字符串
            var connStr = builder.Configuration.GetConnectionString("Default");
            // 仅打印服务器和数据库名，屏蔽密码
            var info = connStr.Split(';');
            var server = info.FirstOrDefault(x => x.StartsWith("Server="));
            var db = info.FirstOrDefault(x => x.StartsWith("Database="));
            Console.WriteLine($"[DEBUG LOG] 启动时连接信息: {server}, {db}");

            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .WriteTo.Async(c => c.AbpStudio(services));
                });
            await builder.AddApplicationAsync<SC_LaborReportingHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
