// Core/Models/CommunicationFlow.cs
namespace RadioIntercepts.Core.Models.Communication
{
    public class CommunicationFlow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<FlowNode> Nodes { get; set; } = new();
        public List<FlowLink> Links { get; set; } = new();
        public List<MessageFlow> MessageFlows { get; set; } = new();
        public Dictionary<string, FlowStatistics> Statistics { get; set; } = new();
    }
}