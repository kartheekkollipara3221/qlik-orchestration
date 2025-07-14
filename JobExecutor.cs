using QlikOrchestration.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QlikOrchestration.Execution
{
    public class JobExecutor
    {
        private readonly string _jobFilePath;

        public JobExecutor(string jobFilePath)
        {
            _jobFilePath = jobFilePath;
        }

        public void Execute(string jobId)
        {
            if (!File.Exists(_jobFilePath))
            {
                Console.WriteLine("Job file not found.");
                return;
            }

            var json = File.ReadAllText(_jobFilePath);
            var jobs = JsonSerializer.Deserialize<List<ReportJob>>(json);

            var job = jobs?.FirstOrDefault(j => j.JobId == jobId);
            if (job == null)
            {
                Console.WriteLine($"Job '{jobId}' not found.");
                return;
            }

            Console.WriteLine($"Executing job: {job.JobId}");

            var executed = new HashSet<string>();

            foreach (var step in job.Steps)
            {
                if (!string.IsNullOrEmpty(step.DependsOn) && !executed.Contains(step.DependsOn))
                {
                    Console.WriteLine($"Cannot run step {step.StepId} yet. Waiting for dependency: {step.DependsOn}");
                    continue;
                }

                Console.WriteLine($"Running step {step.StepId} of type {step.Type}");
                foreach (var param in step.Parameters)
                {
                    Console.WriteLine($"  {param.Key}: {param.Value}");
                }

                executed.Add(step.StepId);
            }

            Console.WriteLine("Job execution finished (no retry/checkpoint yet).");
        }
    }
}