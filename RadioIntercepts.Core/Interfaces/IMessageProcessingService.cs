using RadioIntercepts.Core.Interfaces;
using RadioIntercepts.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Interfaces
{
    public interface IMessageProcessingService
    {
        Task<List<Message>> ProcessAsync(
            IDataSource source,
            IMessageParser parser,
            string sourcePath);
    }
}