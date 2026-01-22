using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.DialogPatterns
{
    public class DialogSequence
    {
        public string SequenceId { get; set; } = null!;
        public List<string> Callsigns { get; set; } = new();
        public List<MessageType> Pattern { get; set; } = new();
        public int Frequency { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public double SuccessRate { get; set; }
    }
}
