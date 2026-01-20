using RadioIntercepts.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RadioIntercepts.Application.Parsers
{
    public class WhatsappRadioMessageParser : IMessageParser
    {
        public Message Parse(string rawMessage)
        {
            var lines = rawMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 5)
                return null;

            var message = new Message
            {
                MessageCallsigns = new List<MessageCallsign>(),
                Unit = "-",
                Dialog = ""
            };

            int lineIndex = 0;

            // 1. Дата сообщения
            if (DateTime.TryParseExact(
                lines[lineIndex].Trim(),
                "dd.MM.yyyy, HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateTime))
            {
                message.DateTime = dateTime;
            }
            lineIndex++;

            // 2. Частота
            if (lineIndex < lines.Length)
            {
                message.Frequency = new Frequency
                {
                    Value = ParseFrequency(lines[lineIndex]) ?? "0"
                };
            }
            lineIndex++;

            // 3. Area (ТОЛЬКО эмодзи из начала строки)
            if (lineIndex < lines.Length)
            {
                var areaLine = lines[lineIndex].Trim();
                var emojiPart = ExtractOnlyEmojisFromStart(areaLine);

                message.Area = new Area
                {
                    Name = emojiPart ?? "❓",
                    Key = ConvertEmojiToKey(emojiPart)
                };
            }
            lineIndex++;

            // 4. Позывные
            while (lineIndex < lines.Length &&
                   IsCallsignLine(lines[lineIndex]) &&
                   message.MessageCallsigns.Count < 2)
            {
                AddCallsigns(message, lines[lineIndex]);
                lineIndex++;
            }

            // 5. Диалог
            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex];

                // Если встретили новую дату (начало нового сообщения) - стоп
                if (IsDateTimeLine(line))
                    break;

                message.Dialog += line + Environment.NewLine;
                lineIndex++;
            }

            message.Dialog = string.IsNullOrWhiteSpace(message.Dialog)
                ? "-"
                : message.Dialog.Trim();

            return message;
        }

        private string ParseFrequency(string line)
        {
            var match = Regex.Match(line, @"(\d+\.\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private string ExtractOnlyEmojisFromStart(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;

            var result = new StringBuilder();

            foreach (var c in line)
            {
                // Если это буква кириллицы или латиницы - стоп
                if ((c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') ||
                    (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    break;
                }

                // Если это цифра - стоп
                if (char.IsDigit(c))
                    break;

                // Если это пробел или скобка - стоп
                if (c == ' ' || c == '(' || c == 'У')
                    break;

                result.Append(c);
            }

            var extracted = result.ToString();

            // Убираем возможные пробелы в конце
            extracted = extracted.Trim();

            return !string.IsNullOrEmpty(extracted) ? extracted : null;
        }

        private string ConvertEmojiToKey(string emoji)
        {
            if (string.IsNullOrEmpty(emoji))
                return "unknown";

            // Преобразуем эмодзи в base64
            byte[] bytes = Encoding.UTF8.GetBytes(emoji);
            return "emoji_" + Convert.ToBase64String(bytes);
        }

        private bool IsCallsignLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();

            if (trimmed.StartsWith("—") || trimmed.StartsWith("--") || trimmed.StartsWith("-"))
                return false;

            return Regex.IsMatch(trimmed, @"^[A-ZА-ЯЇІЄҐ, ]+$");
        }

        private bool IsDateTimeLine(string line)
        {
            return Regex.IsMatch(line.Trim(), @"^\d{2}\.\d{2}\.\d{4},\s*\d{2}:\d{2}:\d{2}$");
        }

        private void AddCallsigns(Message message, string line)
        {
            var parts = line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part) && part.Length >= 2)
                {
                    message.MessageCallsigns.Add(new MessageCallsign
                    {
                        Message = message,
                        Callsign = new Callsign { Name = part.Trim() }
                    });
                }
            }
        }
    }
}