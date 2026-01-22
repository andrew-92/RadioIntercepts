// Core/Models/TemporalAnalysis.cs
namespace RadioIntercepts.Core.Models.TemporalAnalysis
{
    public class TimeSlotAnalysis
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<TimeSlot> Slots { get; set; } = new();
        public TimeSlot PeakSlot { get; set; }
        public TimeSlot QuietSlot { get; set; }
        public double ActivityVariation { get; set; } // Коэффициент вариации активности
    }
}