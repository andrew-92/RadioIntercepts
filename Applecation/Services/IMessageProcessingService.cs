using RadioIntercepts.Application.Parsers;
using RadioIntercepts.Application.Parsers.Sources;
using RadioIntercepts.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioIntercepts.Application.Services
{
    public interface IMessageProcessingService
    {
        Task<List<Message>> ProcessAsync(
            IDataSource source,
            IMessageParser parser,
            string sourcePath);
    }
}