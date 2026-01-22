using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.Graphs
{
    public class GraphNode
    {
        public string Callsign { get; set; } = null!;
        public int TotalMessages { get; set; }
        public double Centrality { get; set; }
        public int Degree => ConnectedEdges?.Count ?? 0;
        public List<GraphEdge> ConnectedEdges { get; set; } = new();
    }
}
