// Core/Models/InteractionGraph.cs
namespace RadioIntercepts.Core.Models
{
    public class InteractionGraph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }

    public class GraphNode
    {
        public string Callsign { get; set; } = null!;
        public int TotalMessages { get; set; }
        public double Centrality { get; set; }
        public int Degree => ConnectedEdges?.Count ?? 0;
        public List<GraphEdge> ConnectedEdges { get; set; } = new();
    }

    public class GraphEdge
    {
        public string SourceCallsign { get; set; } = null!;
        public string TargetCallsign { get; set; } = null!;
        public int Weight { get; set; }
        public DateTime FirstInteraction { get; set; }
        public DateTime LastInteraction { get; set; }
        public double Strength => Weight / (1 + (LastInteraction - FirstInteraction).TotalDays);
    }
}