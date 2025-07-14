using System.Collections.Generic;

namespace QlikOrchestration.Models
{
    public class ReportJob
    {
        public string JobId { get; set; }
        public List<JobStep> Steps { get; set; }
    }

    public class JobStep
    {
        public string StepId { get; set; }
        public string Type { get; set; } // Example: QlikTask, NPrinting, Delivery
        public string DependsOn { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}