using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class FlowNode
    {
        public string Id { get; set; } = null!;
        public string Label { get; set; } = null!;
        public NodeType Type { get; set; }
        public int Size { get; set; } // Размер узла (количество сообщений/взаимодействий)
        public string Color { get; set; } = "#2196F3";
        public List<string> Groups { get; set; } = new();
        public double Centrality { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
