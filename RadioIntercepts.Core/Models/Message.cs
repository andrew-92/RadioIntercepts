
namespace RadioIntercepts.Core.Models
{
    public partial class Message
    {
        public long Id { get; set; }
        public DateTime DateTime { get; set; }

        public int AreaId { get; set; }
        public Area Area { get; set; } = null!;

        public int FrequencyId { get; set; }
        public Frequency Frequency { get; set; } = null!;

        public string? Unit { get; set; }
        public string Dialog { get; set; } = null!;

        public ICollection<MessageCallsign> MessageCallsigns { get; set; } = new List<MessageCallsign>();
    }

}
