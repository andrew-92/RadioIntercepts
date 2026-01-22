using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.SemanticSearch
{
    public class MessageCategory
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<string> Keywords { get; set; } = new();
        public List<string> ExamplePhrases { get; set; } = new();
        public int MessageCount { get; set; }
    }
}
