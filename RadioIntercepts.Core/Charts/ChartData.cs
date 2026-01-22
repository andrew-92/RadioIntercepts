using LiveCharts.Wpf;
using LiveCharts;
using System.Collections.Generic;

namespace RadioIntercepts.Core.Charts
{
    //public class ChartData
    //{
    //    public string Title { get; set; }
    //    public SeriesCollection SeriesCollection { get; set; } = new SeriesCollection();
    //    public List<string> Labels { get; set; } = new List<string>();
    //}

    public class ChartData
    {
        public string Title { get; set; }
        public List<string> Labels { get; set; } = new();
        public List<double> Values { get; set; } = new();
    }

    public class ChartPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }
}