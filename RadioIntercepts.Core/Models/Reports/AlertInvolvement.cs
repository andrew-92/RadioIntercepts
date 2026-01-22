using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class AlertInvolvement
    {
        public string AlertType { get; set; } = null!;
        public int Count { get; set; }
        public DateTime LastInvolvement { get; set; }
        public string Severity { get; set; } = null!;
    }
}
