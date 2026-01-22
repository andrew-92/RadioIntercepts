using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.TemporalPatterns
{
    public class PredictionResult
    {
        public string Callsign { get; set; }
        public List<HourPrediction> Predictions { get; set; }
    }
}
