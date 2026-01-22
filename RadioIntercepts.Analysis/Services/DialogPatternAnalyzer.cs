// Application/Services/DialogPatternAnalyzer.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Core.Interfaces;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RadioIntercepts.Core.Models.DialogPatterns;

namespace RadioIntercepts.Analysis.Services
{
    public class DialogPatternAnalyzer : IDialogPatternAnalyzer
    {
        private readonly AppDbContext _context;

        // Паттерны для определения типа сообщения
        private static readonly Dictionary<MessageType, string[]> MessagePatterns = new()
        {
            [MessageType.Command] = new[]
            {
                "приказ", "команда", "выполнить", "приступить", "начать", "атаковать",
                "отойти", "занять", "следовать", "двигаться", "остановить", "уничтожить"
            },
            [MessageType.Request] = new[]
            {
                "запрос", "прошу", "требуется", "нужно", "необходимо", "помощь",
                "поддержка", "подтвердите", "сообщите", "передайте", "уточните"
            },
            [MessageType.Report] = new[]
            {
                "докладываю", "отчет", "обстановка", "вижу", "обнаружил", "цель",
                "координаты", "пеленг", "расстояние", "состояние", "готовность"
            },
            [MessageType.Confirmation] = new[]
            {
                "понял", "принял", "выполняю", "готов", "согласен", "подтверждаю",
                "ро Roger", "прием", "ясно"
            },
            [MessageType.Query] = new[]
            {
                "повтори", "что", "как", "почему", "где", "когда", "сколько",
                "?", "повторите", "уточните"
            },
            [MessageType.Coordination] = new[]
            {
                "координация", "согласовать", "встреча", "время", "место", "план",
                "синхронизация", "совместно", "координировать"
            },
            [MessageType.Technical] = new[]
            {
                "техника", "топливо", "боеприпасы", "ремонт", "заправка", "заряд",
                "датчик", "система", "оборудование", "радиосвязь", "частота"
            },
            [MessageType.Greeting] = new[]
            {
                "алло", "прием", "вызываю", "добрый", "утро", "день", "вечер",
                "слушаю", "на связи", "здравствуй"
            },
            [MessageType.Farewell] = new[]
            {
                "конец связи", "прием", "все", "до свидания", "пока", "конец",
                "завершаю", "выхожу", "отбой"
            }
        };

        // Признаки ролей
        private static readonly Dictionary<ParticipantRole, string[]> RoleFeatures = new()
        {
            [ParticipantRole.Commander] = new[]
            {
                // Часто использует команды, императив
                "приказ", "команда", "выполнить", "обязан", "требую", "приказываю"
            },
            [ParticipantRole.Executor] = new[]
            {
                // Часто подтверждает, докладывает о выполнении
                "выполняю", "готов", "сделано", "задание", "отчет", "докладываю"
            },
            [ParticipantRole.Observer] = new[]
            {
                // Часто сообщает о наблюдениях
                "вижу", "наблюдаю", "обнаружил", "заметил", "пеленг", "координаты"
            },
            [ParticipantRole.Coordinator] = new[]
            {
                // Координирует действия нескольких сторон
                "координация", "согласовать", "вместе", "параллельно", "синхронно"
            },
            [ParticipantRole.Technician] = new[]
            {
                // Техническая терминология
                "техника", "ремонт", "диагностика", "настройка", "параметры"
            }
        };

        public DialogPatternAnalyzer(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PhrasePattern>> FindCommonPhrasesAsync(int minFrequency = 5)
        {
            var messages = await _context.Messages
                .AsNoTracking()
                .Where(m => !string.IsNullOrWhiteSpace(m.Dialog))
                .Select(m => m.Dialog.ToLower())
                .ToListAsync();

            var phrases = new Dictionary<string, int>();
            var phraseCallsigns = new Dictionary<string, HashSet<string>>();

            foreach (var dialog in messages)
            {
                // Извлекаем предложения
                var sentences = SplitIntoSentences(dialog);

                foreach (var sentence in sentences)
                {
                    // Убираем очень короткие предложения
                    if (sentence.Length < 10) continue;

                    // Нормализуем предложение
                    var normalized = NormalizeText(sentence);

                    if (!phrases.ContainsKey(normalized))
                    {
                        phrases[normalized] = 0;
                        phraseCallsigns[normalized] = new HashSet<string>();
                    }
                    phrases[normalized]++;

                    // Здесь можно добавить логику для связывания с позывными
                    // (пока оставим пустым)
                }
            }

            var result = new List<PhrasePattern>();

            foreach (var phrase in phrases.Where(p => p.Value >= minFrequency))
            {
                var messageType = ClassifySingleMessage(phrase.Key);

                result.Add(new PhrasePattern
                {
                    Phrase = phrase.Key,
                    Frequency = phrase.Value,
                    MessageType = messageType,
                    Confidence = CalculateTypeConfidence(phrase.Key, messageType),
                    AssociatedCallsigns = phraseCallsigns[phrase.Key].ToList()
                });
            }

            return result.OrderByDescending(p => p.Frequency).ThenBy(p => p.Phrase).ToList();
        }

        public async Task<List<DialogSequence>> AnalyzeDialogSequencesAsync(int sequenceLength = 3)
        {
            var result = new List<DialogSequence>();

            // Группируем сообщения по времени (сессии диалога)
            var messages = await _context.Messages
                .AsNoTracking()
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Where(m => m.MessageCallsigns.Any())
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            var sequences = new Dictionary<string, List<Message>>();

            // Разбиваем на последовательности по времени (максимальный разрыв 5 минут)
            var currentSequence = new List<Message>();
            DateTime? lastTime = null;

            foreach (var message in messages)
            {
                if (lastTime.HasValue && (message.DateTime - lastTime.Value).TotalMinutes > 5)
                {
                    if (currentSequence.Count >= sequenceLength)
                    {
                        var seqId = Guid.NewGuid().ToString();
                        sequences[seqId] = new List<Message>(currentSequence);
                    }
                    currentSequence.Clear();
                }

                currentSequence.Add(message);
                lastTime = message.DateTime;
            }

            // Добавляем последнюю последовательность
            if (currentSequence.Count >= sequenceLength)
            {
                var seqId = Guid.NewGuid().ToString();
                sequences[seqId] = new List<Message>(currentSequence);
            }

            // Анализируем каждую последовательность
            foreach (var sequence in sequences.Values)
            {
                var callsigns = sequence
                    .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    .Distinct()
                    .ToList();

                var pattern = sequence
                    .Select(m => ClassifySingleMessage(m.Dialog))
                    .ToList();

                // Анализируем успешность (есть ли подтверждения после команд)
                int successCount = 0;
                int commandCount = 0;

                for (int i = 0; i < pattern.Count - 1; i++)
                {
                    if (pattern[i] == MessageType.Command || pattern[i] == MessageType.Request)
                    {
                        commandCount++;
                        // Ищем подтверждение в следующих сообщениях
                        for (int j = i + 1; j < Math.Min(i + 4, pattern.Count); j++)
                        {
                            if (pattern[j] == MessageType.Confirmation)
                            {
                                successCount++;
                                break;
                            }
                        }
                    }
                }

                var dialogSequence = new DialogSequence
                {
                    SequenceId = Guid.NewGuid().ToString(),
                    Callsigns = callsigns,
                    Pattern = pattern,
                    Frequency = 1, // Эта конкретная последовательность встречается 1 раз
                    AverageDuration = TimeSpan.FromMinutes((sequence.Last().DateTime - sequence.First().DateTime).TotalMinutes / sequence.Count),
                    SuccessRate = commandCount > 0 ? (double)successCount / commandCount : 0
                };

                result.Add(dialogSequence);
            }

            // Группируем похожие последовательности
            var groupedSequences = result
                .GroupBy(s => string.Join("->", s.Pattern.Select(p => p.ToString())))
                .Select(g => new DialogSequence
                {
                    SequenceId = g.Key,
                    Callsigns = g.SelectMany(s => s.Callsigns).Distinct().ToList(),
                    Pattern = g.First().Pattern,
                    Frequency = g.Count(),
                    AverageDuration = TimeSpan.FromMinutes(g.Average(s => s.AverageDuration.TotalMinutes)),
                    SuccessRate = g.Average(s => s.SuccessRate)
                })
                .ToList();

            return groupedSequences.OrderByDescending(s => s.Frequency).ThenByDescending(s => s.SuccessRate).ToList();
        }

        public async Task<List<RoleAnalysisResult>> AnalyzeRolesAsync()
        {
            var result = new List<RoleAnalysisResult>();

            // Получаем все позывные
            var callsigns = await _context.Callsigns
                .AsNoTracking()
                .Select(c => c.Name)
                .ToListAsync();

            foreach (var callsign in callsigns)
            {
                // Получаем все сообщения этого позывного
                var messages = await _context.MessageCallsigns
                    .Include(mc => mc.Message)
                    .Where(mc => mc.Callsign.Name == callsign)
                    .Select(mc => mc.Message)
                    .ToListAsync();

                if (!messages.Any()) continue;

                // Анализируем типы сообщений
                var typeDistribution = new Dictionary<MessageType, int>();
                foreach (var type in Enum.GetValues(typeof(MessageType)).Cast<MessageType>())
                {
                    typeDistribution[type] = 0;
                }

                foreach (var message in messages)
                {
                    var type = ClassifySingleMessage(message.Dialog);
                    typeDistribution[type]++;
                }

                // Рассчитываем признаки роли
                var roleFeatures = new Dictionary<string, double>();

                foreach (var role in RoleFeatures.Keys)
                {
                    double featureScore = 0;
                    var features = RoleFeatures[role];

                    foreach (var feature in features)
                    {
                        // Считаем, сколько раз признаки роли встречаются в сообщениях
                        foreach (var message in messages)
                        {
                            if (message.Dialog.ToLower().Contains(feature))
                            {
                                featureScore += 1.0;
                            }
                        }
                    }

                    roleFeatures[role.ToString()] = featureScore / messages.Count;
                }

                // Определяем наиболее вероятную роль
                var mostLikelyRole = roleFeatures
                    .OrderByDescending(rf => rf.Value)
                    .FirstOrDefault();

                var roleEnum = mostLikelyRole.Key != null
                    ? (ParticipantRole)Enum.Parse(typeof(ParticipantRole), mostLikelyRole.Key)
                    : ParticipantRole.Unknown;

                result.Add(new RoleAnalysisResult
                {
                    Callsign = callsign,
                    Role = roleEnum,
                    RoleConfidence = mostLikelyRole.Value,
                    MessageTypeDistribution = typeDistribution,
                    RoleFeatures = roleFeatures
                });
            }

            return result.OrderByDescending(r => r.RoleConfidence).ToList();
        }

        public async Task<Dictionary<string, MessageType>> ClassifyMessagesAsync(
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Messages.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.DateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.DateTime <= endDate.Value);

            var messages = await query
                .Select(m => new { m.Id, m.Dialog })
                .ToListAsync();

            var result = new Dictionary<string, MessageType>();

            foreach (var message in messages)
            {
                result[message.Id.ToString()] = ClassifySingleMessage(message.Dialog);
            }

            return result;
        }

        public async Task<List<string>> ExtractKeywordsAsync(string dialog, int topN = 10)
        {
            // Удаляем стоп-слова
            var stopWords = new HashSet<string>
            {
                "и", "в", "на", "с", "по", "для", "не", "что", "это", "как",
                "вот", "так", "но", "а", "или", "же", "то", "из", "у", "к",
                "да", "нет", "ой", "ах", "эх", "ну", "все", "еще", "уже"
            };

            var words = dialog.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '[', ']' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .GroupBy(w => w)
                .Select(g => new { Word = g.Key, Count = g.Count() })
                .OrderByDescending(w => w.Count)
                .Take(topN)
                .Select(w => w.Word)
                .ToList();

            return words;
        }

        public async Task<Dictionary<string, double>> CalculateStyleMetricsAsync(string callsign)
        {
            var metrics = new Dictionary<string, double>();

            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message.Dialog)
                .ToListAsync();

            if (!messages.Any())
                return metrics;

            // 1. Средняя длина сообщения
            var avgLength = messages.Average(m => m.Length);
            metrics["AvgMessageLength"] = avgLength;

            // 2. Доля вопросов
            var questionCount = messages.Count(m => m.Contains('?'));
            metrics["QuestionRatio"] = (double)questionCount / messages.Count;

            // 3. Доля восклицательных предложений
            var exclamationCount = messages.Count(m => m.Contains('!'));
            metrics["ExclamationRatio"] = (double)exclamationCount / messages.Count;

            // 4. Доля числовых данных (координаты, время, количество)
            var digitRegex = new Regex(@"\d+");
            var digitCount = messages.Count(m => digitRegex.IsMatch(m));
            metrics["DigitRatio"] = (double)digitCount / messages.Count;

            // 5. Уникальность словарного запаса
            var allWords = messages
                .SelectMany(m => m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(w => w.ToLower())
                .Distinct()
                .Count();
            metrics["VocabularySize"] = allWords;

            // 6. Коэффициент повторения слов
            var totalWords = messages
                .SelectMany(m => m.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Count();
            metrics["WordRepetition"] = totalWords > 0 ? (double)allWords / totalWords : 0;

            return metrics;
        }

        // Вспомогательные методы

        private MessageType ClassifySingleMessage(string dialog)
        {
            if (string.IsNullOrWhiteSpace(dialog))
                return MessageType.Unknown;

            var lowerDialog = dialog.ToLower();
            var scores = new Dictionary<MessageType, int>();

            foreach (var type in MessagePatterns.Keys)
            {
                scores[type] = 0;
                foreach (var pattern in MessagePatterns[type])
                {
                    if (lowerDialog.Contains(pattern))
                    {
                        scores[type]++;
                    }
                }
            }

            // Если нет совпадений, пытаемся определить по структуре
            if (scores.Values.Sum() == 0)
            {
                if (lowerDialog.Contains('?'))
                    return MessageType.Query;
                if (lowerDialog.Contains('!'))
                    return MessageType.Command;
                if (lowerDialog.Length < 10)
                    return MessageType.Confirmation;

                return MessageType.Unknown;
            }

            return scores.OrderByDescending(s => s.Value).First().Key;
        }

        private double CalculateTypeConfidence(string text, MessageType type)
        {
            if (type == MessageType.Unknown)
                return 0.0;

            var lowerText = text.ToLower();
            var patterns = MessagePatterns[type];
            int matches = 0;

            foreach (var pattern in patterns)
            {
                if (lowerText.Contains(pattern))
                {
                    matches++;
                }
            }

            return Math.Min(1.0, (double)matches / patterns.Length * 2);
        }

        private List<string> SplitIntoSentences(string text)
        {
            // Простой разделитель предложений
            return text.Split(new[] { '.', '!', '?', ';', '\n' },
                             StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .ToList();
        }

        private string NormalizeText(string text)
        {
            // Приводим к нижнему регистру, удаляем лишние пробелы
            return text.ToLower().Trim();
        }
    }
}