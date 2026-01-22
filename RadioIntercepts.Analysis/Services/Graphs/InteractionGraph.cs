using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.Graphs
{
    public class InteractionGraph
    {
        public List<GraphNode> Nodes { get; set; }
        public List<GraphEdge> Edges { get; set; }
    }
}
