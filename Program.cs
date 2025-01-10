using FF.Drawing;
using FF.Picking;
using FF.TasksData;
using FF.WarehouseData;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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