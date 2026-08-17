using System;

namespace AURA.Core.Events
{
    public sealed class OrchestrationStepEvent : IEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Target { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public double Progress { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
