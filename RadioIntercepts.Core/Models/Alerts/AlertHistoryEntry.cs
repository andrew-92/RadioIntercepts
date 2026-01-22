using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Alerts
{
    public class AlertHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = null!;
        public string User { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Details { get; set; } = null!;
    }
}
