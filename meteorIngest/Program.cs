using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeteorIngestAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace meteorIngest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("before run");
            CreateHostBuilder(args).Build().Run();
            Console.WriteLine("after run");
            //loop


        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
