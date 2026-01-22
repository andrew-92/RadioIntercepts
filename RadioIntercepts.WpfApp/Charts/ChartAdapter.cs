using LiveCharts;
using LiveCharts.Wpf;
using RadioIntercepts.Core.Charts;
using System.Linq;

namespace RadioIntercepts.WpfApp.Charts
{
    public static class ChartAdapter
    {
        public static SeriesCollection ToColumnSeries(ChartData data)
        {
            if (data == null || data.Values.Count == 0)
                return new SeriesCollection();

            return new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = data.Title,
                    Values = new ChartValues<double>(data.Values)
                }
            };
        }

        public static string[] ToLabels(ChartData data)
        {
            return data?.Labels?.ToArray() ?? new string[0];
        }
    }
}
