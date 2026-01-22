using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class CallsignDossier
    {
        public string Callsign { get; set; } = null!;
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int TotalMessages { get; set; }
        public List<string> FrequentInterlocutors { get; set; } = new();
        public List<string> ActiveAreas { get; set; } = new();
        public CommunicationStyle Style { get; set; } = new();
        public RoleAnalysis Role { get; set; } = new();
        public List<KeyInteraction> KeyInteractions { get; set; } = new();
        public List<PatternParticipation> PatternInvolvement { get; set; } = new();
        public List<AlertInvolvement> Alerts { get; set; } = new();
        public List<BehavioralChange> BehavioralChanges { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }
}
