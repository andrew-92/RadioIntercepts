using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Core.Interfaces
{
    public interface IMessageParser
    {
        Message Parse(string rawMessage);
    }
}