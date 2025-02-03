using System.Text;
using Application;
using Domain.Interfaces;
using FF.Drawing;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Json;

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
            System.Windows.Forms.Application.Run(
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
                    rollingInterval: RollingInterval.Minute,
                    encoding: Encoding.UTF8)
                .CreateLogger();

            services.AddServices();
            services.AddInfrastructure();

            services
                .AddScoped<Form1>()
                .AddSingleton<IDrawingService, DrawingService>();
        }
    }
}