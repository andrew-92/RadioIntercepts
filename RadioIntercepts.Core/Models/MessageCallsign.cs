
namespace RadioIntercepts.Core.Models
{
    public class MessageCallsign
    {
        public long MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public int CallsignId { get; set; }
        public Callsign Callsign { get; set; } = null!;
    }

}
