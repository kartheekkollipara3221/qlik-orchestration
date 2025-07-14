using QlikOrchestration.Models;
using QlikOrchestration.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace QlikOrchestration.Execution
{
    public class JobExecutor
    {
        private readonly string _jobFilePath;
        private readonly string _checkpointDir;
        private readonly List<IStepRunner> _runners;

        public JobExecutor(string jobFilePath, string checkpointDir, List<IStepRunner> runners)
        {
            _jobFilePath = jobFilePath;
            _checkpointDir = checkpointDir;
            _runners = runners;
        }

        public void Execute(string jobId)
        {
            // Step 1: Load job definitions
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

            // Step 2: Build execution order (topological sort)
            var executionOrder = GetExecutionOrder(job.Steps);
            if (executionOrder == null)
            {
                Console.WriteLine("Failed to build execution plan. Check for circular or missing dependencies.");
                return;
            }

            // Step 3: Load checkpoint file to skip completed steps
            var checkpoint = new CheckpointManager(job.JobId, _checkpointDir);
            var completedSteps = checkpoint.Load();

            // Step 4: Execute each step in order
            foreach (var step in executionOrder)
            {
                if (completedSteps.Contains(step.StepId))
                {
                    Console.WriteLine($"Skipping step {step.StepId} (already completed)");
                    continue;
                }

                var runner = _runners.FirstOrDefault(r => r.CanHandle(step.Type));
                if (runner == null)
                {
                    Console.WriteLine($"No runner found for step type: {step.Type}");
                    continue;
                }

                bool success = false;
                int attempts = 0;

                while (!success && attempts < 3)
                {
                    try
                    {
                        Console.WriteLine($"Running step {step.StepId} (attempt {attempts + 1})");
                        runner.Run(step);

                        // Step 5: Mark step complete and update checkpoint
                        completedSteps.Add(step.StepId);
                        checkpoint.Save(completedSteps);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        attempts++;
                        Console.WriteLine($"Step {step.StepId} failed: {ex.Message}");

                        if (attempts < 3)
                        {
                            Console.WriteLine("Retrying...");
                            Thread.Sleep(2000);
                        }
                    }
                }

                if (!success)
                {
                    Console.WriteLine($"Step {step.StepId} failed after 3 attempts. Aborting job.");
                    return;
                }
            }

            Console.WriteLine("Job execution finished.");
        }

        // Helper method: Build execution order via topological sort
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