using RadioIntercepts.Core.Interfaces;
using System.IO;

namespace RadioIntercepts.Application.Parsers.Source
{
    public class FileRadioMessageSource : IRadioMessageSource
    {
        private readonly string _filePath;

        public FileRadioMessageSource(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<string> GetRawMessageAsync()
        {
            return await File.ReadAllTextAsync(_filePath);
        }
    }
}
