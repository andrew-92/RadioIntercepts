using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.TemporalAnalysis
{
    public class PredictionResult
    {
        public DateTime PredictedTime { get; set; }
        public double Probability { get; set; }
        public string PredictedEvent { get; set; } = null!; // "Активность позывного X", "Сообщение в зоне Y"
        public double Confidence { get; set; }
    }
}
