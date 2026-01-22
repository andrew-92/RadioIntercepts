using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Interfaces
{
    public interface IDataSource
    {
        Task<IEnumerable<string>> ExtractRawMessagesAsync(string source);
    }
}