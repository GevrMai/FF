using System.Text;
using FF.Drawing;
using FF.Picking;
using FF.TasksData;
using FF.WarehouseData;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace FF
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Метрики с использованием Прометеус
            using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("Picking")
                .AddPrometheusHttpListener(options => options.UriPrefixes = new [] { "http://localhost:9184/" })
                .Build();
            
            ApplicationConfiguration.Initialize();
            
            var services = new ServiceCollection();  
            ConfigureServices(services); 
            
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            Application.Run(
                serviceProvider.GetRequiredService<Form1>()
                );  
        }
        
        private static void ConfigureServices(ServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs.txt",
                    formatter: new JsonFormatter(),
                    retainedFileCountLimit: 10_000_000,
                    rollingInterval: RollingInterval.Day,
                    encoding: Encoding.UTF8)
                .CreateLogger();

            services
                .AddScoped<Form1>()
                .AddSingleton<TaskService>()
                .AddSingleton<WarehouseTopology>()
                .AddSingleton<DrawingService>()
                .AddSingleton<DefaultPicking>()
                .AddSingleton<OptimizedPicking>()
                .AddSingleton<PathFinder>();
        }
    }
}