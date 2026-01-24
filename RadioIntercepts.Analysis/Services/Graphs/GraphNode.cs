using RadioIntercepts.Analysis.Services.Graphs;

public class GraphNode
{
    public string Callsign { get; set; } = null!;
    public int TotalMessages { get; set; }
    public double Centrality { get; set; }
    public double DegreeCentrality { get; set; }
    public double BetweennessCentrality { get; set; }
    public double ClosenessCentrality { get; set; }
    public int Degree => ConnectedEdges?.Count ?? 0;
    public List<GraphEdge> ConnectedEdges { get; set; } = new();
    public int CommunityId { get; set; } = -1;
}