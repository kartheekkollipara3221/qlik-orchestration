using Microsoft.Extensions.Configuration;
using QlikOrchestration.Execution;
using System;
using System.IO;
using System.Linq;

namespace QlikOrchestration
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Qlik Orchestration...");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Configs/appsettings.json", optional: false)
                .Build();

            string jobId = args.FirstOrDefault(a => a.StartsWith("--job="))?.Split("=")[1];

            if (string.IsNullOrWhiteSpace(jobId))
            {
                Console.WriteLine("Missing --job parameter.");
                return;
            }

            var jobFilePath = Path.Combine("Configs", "ReportJobs.json");
            var executor = new JobExecutor(jobFilePath);
            executor.Execute(jobId);
        }
    }
}