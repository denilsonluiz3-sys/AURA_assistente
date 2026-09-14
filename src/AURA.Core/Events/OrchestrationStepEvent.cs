using System;

namespace AURA.Core.Events
{
    public sealed class OrchestrationStepEvent : IEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double Progress { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
