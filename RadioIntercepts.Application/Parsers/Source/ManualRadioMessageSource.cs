using RadioIntercepts.Core.Interfaces;

namespace RadioIntercepts.Application.Parsers.Source
{
    public class ManualRadioMessageSource : IRadioMessageSource
    {
        private readonly string _input;

        public ManualRadioMessageSource(string input)
        {
            _input = input;
        }

        public Task<string> GetRawMessageAsync()
        {
            return Task.FromResult(_input);
        }
    }
}
