using RadioIntercepts.Analysis.Models;
using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Analysis
{
    public interface ICallsignNetworkAnalyzer
    {
        IReadOnlyList<CallsignLink> BuildNetwork(
            IEnumerable<Message> messages,
            DateTime? from = null,
            DateTime? to = null);
    }
}
