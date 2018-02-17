using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DE_Sharp.WebStarter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .ConfigureAppConfiguration((hostingContext, config) =>
                   {
                        var env = hostingContext.HostingEnvironment;
                        var settings = Directory.GetFiles("./", "settings.*.json");
                        settings.ToList().ForEach(setting =>
                        {
                            config.AddJsonFile(setting, optional: false, reloadOnChange: true);
                        });

                        config.AddEnvironmentVariables();

                        if (args != null)
                        {
                            config.AddCommandLine(args);
                        }
                   })
                .UseUrls("http://localhost:8800")
                .UseStartup<Startup>()
                .Build();
    }
}
