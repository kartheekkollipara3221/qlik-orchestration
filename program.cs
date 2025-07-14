using Microsoft.Extensions.Configuration;
using System;
using System.IO;

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

            string logDir = config["LogDirectory"];
            string checkpointDir = config["CheckpointDirectory"];
            string retention = config["RetentionDays"];

            Console.WriteLine($"LogDirectory: {logDir}");
            Console.WriteLine($"CheckpointDirectory: {checkpointDir}");
            Console.WriteLine($"RetentionDays: {retention}");
        }
    }
}