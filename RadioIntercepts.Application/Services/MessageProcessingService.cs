using RadioIntercepts.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadioIntercepts.Core.Interfaces;

namespace RadioIntercepts.Application.Services
{
    public class MessageProcessingService : IMessageProcessingService
    {
        public async Task<List<Message>> ProcessAsync(
            IDataSource source,
            IMessageParser parser,
            string sourcePath)
        {
            var messages = new List<Message>();

            // 1. Извлекаем сырые сообщения из источника
            var rawMessages = await source.ExtractRawMessagesAsync(sourcePath);

            // 2. Парсим каждое сообщение
            foreach (var raw in rawMessages)
            {
                var message = parser.Parse(raw);
                if (message != null)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }
    }
}