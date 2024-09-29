using FF.Drawing;
using FF.Picking;
using FF.TasksData;
using FF.WarehouseData;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddScoped<Form1>();
            services.AddSingleton<TaskService>();
            services.AddSingleton<WarehouseTopology>();
            services.AddSingleton<DrawingService>();
            services.AddSingleton<DefaultPicking>();
            services.AddSingleton<OptimizedPicking>();
            services.AddSingleton<PathFinder>();
        }
    }
}