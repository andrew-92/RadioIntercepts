using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class ParallelTimeline
    {
        public List<TimelineTrack> Tracks { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, TimelineStatistics> TrackStats { get; set; } = new();
    }
}
