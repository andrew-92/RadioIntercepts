using RadioIntercepts.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.SemanticSearch
{
    public class MessageCluster
    {
        public int Id { get; set; }
        public List<string> Keywords { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
        public int Size { get; set; }
        public double AverageSimilarity { get; set; }
        public string Description => $"Кластер {Id}: {string.Join(", ", Keywords.Take(3))}...";
    }
}
