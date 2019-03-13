using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace ProcessManager.ProgramManager
{
    class Program
    {
        static public IConfigurationRoot Configuration { get; set; }

        public static void Main(string[] args)
        {
            try
            {
                Configuration = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .Build();

                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseConfiguration(Configuration)
                .UseStartup<Startup>();
    }
}
