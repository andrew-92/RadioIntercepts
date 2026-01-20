
namespace RadioIntercepts.Application.Parsers.Sources
{
    public interface IRadioMessageSource
    {
        Task<string> GetRawMessageAsync();
    }
}
