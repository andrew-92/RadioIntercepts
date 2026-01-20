using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Application.Parsers
{
    public interface IMessageParser
    {
        Message Parse(string rawMessage);
    }
}