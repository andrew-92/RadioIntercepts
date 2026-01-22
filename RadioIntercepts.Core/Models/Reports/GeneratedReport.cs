using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class GeneratedReport
    {
        public string ReportId { get; set; } = null!;
        public string TemplateName { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!; // "pdf", "html", "csv", "excel", "word"
        public string FileName { get; set; } = null!;
        public ReportStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
