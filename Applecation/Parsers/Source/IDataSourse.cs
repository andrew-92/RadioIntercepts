using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioIntercepts.Application.Parsers.Sources
{
    public interface IDataSource
    {
        Task<IEnumerable<string>> ExtractRawMessagesAsync(string source);
    }
}