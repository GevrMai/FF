using System.Text;
using FF.Drawing;
using FF.Picking;
using FF.TasksData;
using FF.WarehouseData;
using Microsoft.Extensions.DependencyInjection;
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
                    retainedFileCountLimit: 100,
                    rollingInterval: RollingInterval.Minute,
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