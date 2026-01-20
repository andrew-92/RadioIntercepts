using RadioIntercepts.Analysis.Models;
using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Analysis
{
    public class CallsignNetworkAnalyzer
    {
        public IReadOnlyList<CallsignLink> BuildNetwork(
            IEnumerable<Message> messages,
            DateTime? from = null,
            DateTime? to = null)
        {
            var filtered = messages;

            if (from.HasValue)
                filtered = filtered.Where(m => m.DateTime >= from.Value);

            if (to.HasValue)
                filtered = filtered.Where(m => m.DateTime <= to.Value);

            var pairs = filtered
                .Where(m => m.MessageCallsigns.Count >= 2)
                .SelectMany(m =>
                {
                    var sender = Normalize(m.MessageCallsigns.First().Callsign);
                    if (sender == null)
                        return Enumerable.Empty<(string From, string To, DateTime Time)>();

                    return m.MessageCallsigns
                        .Skip(1)
                        .Select(c => Normalize(c.Callsign))
                        .Where(r => r != null)
                        .Select(r => (
                            From: sender,
                            To: r!,
                            Time: m.DateTime
                        ));
                });

            return pairs
                .GroupBy(x => new { x.From, x.To })
                .Select(g => new CallsignLink
                {
                    FromCallsign = g.Key.From,
                    ToCallsign = g.Key.To,
                    MessageCount = g.Count(),
                    FirstSeen = g.Min(x => x.Time),
                    LastSeen = g.Max(x => x.Time)
                })
                .OrderByDescending(l => l.MessageCount)
                .ToList();
        }

        private static string? Normalize(object? callsign)
        {
            if (callsign == null)
                return null;

            var value = callsign.ToString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().ToUpperInvariant();
        }
    }
}
