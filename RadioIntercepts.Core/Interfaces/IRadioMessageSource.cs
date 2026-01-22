namespace RadioIntercepts.Core.Interfaces
{
    public interface IRadioMessageSource
    {
        Task<string> GetRawMessageAsync();
    }
}
