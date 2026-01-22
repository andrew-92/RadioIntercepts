using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.SemanticSearch
{
    public class SearchByExampleRequest
    {
        public string ExampleText { get; set; } = null!;
        public int MaxSimilarExamples { get; set; } = 5;
        public bool IncludeOpposite { get; set; } = false;
    }
}
