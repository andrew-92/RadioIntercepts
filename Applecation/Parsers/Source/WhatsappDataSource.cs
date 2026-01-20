using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadioIntercepts.Application.Parsers.Sources
{
    public class WhatsappDataSource : IDataSource
    {
        public async Task<IEnumerable<string>> ExtractRawMessagesAsync(string filePath)
        {
            var messages = new List<string>();
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            var regex = new Regex(@"^\[(\d{2}\.\d{2}\.\d{4}, \d{2}:\d{2}:\d{2})\]");

            var currentMessage = new StringBuilder();

            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    if (currentMessage.Length > 0)
                    {
                        messages.Add(currentMessage.ToString());
                        currentMessage.Clear();
                    }

                    // Отрезаем [дата, время] отправитель:
                    string remainder = line.Substring(match.Length).TrimStart();

                    // Удаляем имя отправителя до двоеточия
                    int colonIndex = remainder.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        remainder = remainder.Substring(colonIndex + 1).TrimStart();
                    }

                    currentMessage.AppendLine(remainder);
                }
                else
                {
                    currentMessage.AppendLine(line);
                }
            }

            if (currentMessage.Length > 0)
            {
                messages.Add(currentMessage.ToString());
            }

            return messages;
        }
    }
}