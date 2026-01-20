namespace RadioIntercepts.Analysis.Models
{
    public class CallsignLink
    {
        public string FromCallsign { get; set; } = string.Empty;
        public string ToCallsign { get; set; } = string.Empty;

        public int MessageCount { get; set; }

        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
