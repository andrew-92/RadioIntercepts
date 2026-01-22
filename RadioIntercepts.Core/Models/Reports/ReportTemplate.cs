// Core/Models/Reports.cs
using RadioIntercepts.Core.Models.Alerts;
using RadioIntercepts.Core.Models.Communication;

namespace RadioIntercepts.Core.Models.Reports
{
    public class ReportTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ReportType Type { get; set; }
        public string TemplatePath { get; set; } = null!;
        public List<ReportParameter> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}