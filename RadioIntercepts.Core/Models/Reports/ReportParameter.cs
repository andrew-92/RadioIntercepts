using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class ReportParameter
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // "date", "string", "int", "bool", "list"
        public string DefaultValue { get; set; } = null!;
        public bool Required { get; set; }
        public List<string> Options { get; set; } = new();
    }
}
