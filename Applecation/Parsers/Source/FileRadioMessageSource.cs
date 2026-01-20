using System.IO;

namespace RadioIntercepts.Application.Parsers.Sources
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
