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

            var executionOrder = GetExecutionOrder(job.Steps);
            if (executionOrder == null)
            {
                Console.WriteLine("Failed to build execution plan. Check for circular or missing dependencies.");
                return;
            }

            foreach (var step in executionOrder)
            {
                Console.WriteLine($"Running step {step.StepId} of type {step.Type}");
                foreach (var param in step.Parameters)
                {
                    Console.WriteLine($"  {param.Key}: {param.Value}");
                }
            }

            Console.WriteLine("Job execution finished.");
        }

        private List<JobStep> GetExecutionOrder(List<JobStep> steps)
        {
            var stepMap = steps.ToDictionary(s => s.StepId);
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();
            var result = new List<JobStep>();

            bool Dfs(string stepId)
            {
                if (visited.Contains(stepId))
                    return true;
                if (visiting.Contains(stepId))
                    return false; // cycle detected

                visiting.Add(stepId);
                var step = stepMap[stepId];

                if (!string.IsNullOrWhiteSpace(step.DependsOn))
                {
                    if (!stepMap.ContainsKey(step.DependsOn))
                    {
                        Console.WriteLine($"Missing dependency: {step.DependsOn}");
                        return false;
                    }
                    if (!Dfs(step.DependsOn))
                        return false;
                }

                visiting.Remove(stepId);
                visited.Add(stepId);
                result.Add(step);
                return true;
            }

            foreach (var step in steps)
            {
                if (!visited.Contains(step.StepId))
                {
                    if (!Dfs(step.StepId))
                        return null;
                }
            }

            return result;
        }
    }
}