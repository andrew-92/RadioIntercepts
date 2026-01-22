using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.Analysis.Interfaces.Services;
using RadioIntercepts.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RadioIntercepts.Core.Models.CodeAnalysis;

namespace RadioIntercepts.Analysis.Services
{
    public class CodeAnalysisService : ICodeAnalysisService
    {
        private readonly AppDbContext _context;
        private readonly IDialogPatternAnalyzer _dialogAnalyzer;

        // Предопределенные словари терминов (можно расширять)
        private static readonly Dictionary<CodeTermCategory, string[]> _predefinedTerms = new()
        {
            [CodeTermCategory.Military] = new[]
            {
                "цель", "объект", "противник", "свои", "нейтралы", "позиция",
                "фланг", "тыл", "авангард", "арьергард", "дозор", "застава",
                "окоп", "бункер", "укрепление", "мина", "снаряд", "патрон"
            },
            [CodeTermCategory.Slang] = new[]
            {
                "колобок", "еж", "медведь", "волк", "лиса", "заяц", "птичка",
                "рыба", "змея", "паук", "муха", "комар", "жук", "бабочка",
                "цветок", "дерево", "камень", "песок", "вода", "огонь"
            },
            [CodeTermCategory.CodeWord] = new[]
            {
                "альфа", "браво", "чарли", "дельта", "эхо", "фокстрот", "гольф",
                "отель", "индия", "юлиет", "кило", "лима", "майк", "ноябрь",
                "оскар", "папа", "квебек", "ромео", "сьерра", "танго", "униформ",
                "виктор", "виски", "икс-рей", "янки", "зулу"
            },
            [CodeTermCategory.Abbreviation] = new[]
            {
                "БТР", "БМП", "Т-72", "Т-80", "Т-90", "САУ", "ПТУР", "ЗРК", "РЛС",
                "КНП", "НП", "ОП", "ДОТ", "ДЗОТ", "РХБЗ", "МТО", "МО", "ГСМ"
            },
            [CodeTermCategory.Technical] = new[]
            {
                "частота", "канал", "модуляция", "антенна", "усилитель", "приемник",
                "передатчик", "радиостанция", "аккумулятор", "генератор", "компас",
                "навигатор", "дальномер", "тепловизор", "прицел", "стабилизатор"
            },
            [CodeTermCategory.Location] = new[]
            {
                "север", "юг", "запад", "восток", "центр", "периметр", "квартал",
                "сектор", "квадрат", "зона", "район", "направление", "коридор",
                "перекресток", "развилка", "высота", "овраг", "лес", "поле"
            },
            [CodeTermCategory.Equipment] = new[]
            {
                "броня", "ствол", "башня", "гусеница", "колесо", "двигатель",
                "трансмиссия", "оптика", "прибор", "датчик", "экран", "пульт",
                "кабель", "разъем", "инструмент", "запчасть", "комплект"
            },
            [CodeTermCategory.Operation] = new[]
            {
                "захват", "удержание", "блокирование", "обход", "охват", "прорыв",
                "отражение", "контратака", "зачистка", "разведка", "наблюдение",
                "маскировка", "радиомолчание", "радиообмен", "координация"
            },
            [CodeTermCategory.Urgency] = new[]
            {
                "срочно", "немедленно", "быстро", "медленно", "осторожно",
                "опасно", "критично", "важно", "приоритет", "внеочередной"
            }
        };

        // Шаблоны для обнаружения сленга
        private static readonly (string Pattern, string Meaning)[] _slangPatterns = new[]
        {
            (@"\b[А-Я]{2,3}-\d{2,3}\b", "Обозначение техники (например, Т-72)"),
            (@"\b[А-Я]{1,2}\d{3,4}\b", "Обозначение позиции или квадрата"),
            (@"\b\d{1,2}[-:]\d{1,2}\b", "Временные обозначения"),
            (@"\b[А-Я]{2,}\s*[+-]\s*[А-Я]{2,}\b", "Совместные операции"),
            (@"\b[а-я]+\-[а-я]+\b", "Составные сленговые слова"),
            (@"\b[А-Я]{3,}\b", "Аббревиатуры (3+ заглавных букв)"),
            (@"\b[а-я]{1,3}\d+[а-я]*\b", "Кодовые обозначения с цифрами"),
        };

        public CodeAnalysisService(AppDbContext context, IDialogPatternAnalyzer dialogAnalyzer)
        {
            _context = context;
            _dialogAnalyzer = dialogAnalyzer;
        }

        public async Task<List<CodeTerm>> ExtractCodeTermsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var terms = new List<CodeTerm>();

            // Получаем все сообщения за указанный период
            var query = _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.DateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.DateTime <= endDate.Value);

            var messages = await query.ToListAsync();

            if (!messages.Any())
                return terms;

            // Собираем статистику по всем словам
            var wordStats = new Dictionary<string, WordStatistics>();
            var totalWords = 0;

            foreach (var message in messages)
            {
                var words = ExtractWords(message.Dialog);
                totalWords += words.Count;

                foreach (var word in words)
                {
                    if (!wordStats.ContainsKey(word))
                    {
                        wordStats[word] = new WordStatistics
                        {
                            Word = word,
                            Callsigns = new HashSet<string>(),
                            Areas = new HashSet<string>(),
                            Messages = new HashSet<long>()
                        };
                    }

                    var stats = wordStats[word];
                    stats.Frequency++;

                    foreach (var callsign in message.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    {
                        stats.Callsigns.Add(callsign);
                    }

                    stats.Areas.Add(message.Area.Name);
                    stats.Messages.Add(message.Id);

                    if (message.DateTime < stats.FirstSeen)
                        stats.FirstSeen = message.DateTime;
                    if (message.DateTime > stats.LastSeen)
                        stats.LastSeen = message.DateTime;
                }
            }

            // Определяем категории и создаем CodeTerm объекты
            foreach (var stats in wordStats.Values)
            {
                if (stats.Frequency < 3) // Игнорируем редкие слова
                    continue;

                var category = DetermineTermCategory(stats.Word);
                if (category == CodeTermCategory.Unknown && stats.Frequency < 10)
                    continue; // Игнорируем редкие неизвестные термины

                var distinctiveness = CalculateDistinctiveness(stats, wordStats.Values);

                var term = new CodeTerm
                {
                    Term = stats.Word,
                    Category = category,
                    FirstSeen = stats.FirstSeen,
                    LastSeen = stats.LastSeen,
                    FrequencyScore = (double)stats.Frequency / totalWords,
                    DistinctivenessScore = distinctiveness,
                    IsActive = (DateTime.UtcNow - stats.LastSeen).TotalDays < 30,
                    TypicalContexts = await ExtractTypicalContextsAsync(stats.Word, messages)
                };

                terms.Add(term);
            }

            return terms
                .OrderByDescending(t => t.FrequencyScore * t.DistinctivenessScore)
                .ThenByDescending(t => t.LastSeen)
                .ToList();
        }

        public async Task<List<CodeUsageStatistic>> GetCodeUsageStatisticsAsync(string? term = null, CodeTermCategory? category = null)
        {
            var statistics = new List<CodeUsageStatistic>();

            // Получаем все сообщения
            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .ToListAsync();

            // Получаем все термины или фильтруем по параметрам
            var termsQuery = await ExtractCodeTermsAsync();

            if (!string.IsNullOrEmpty(term))
                termsQuery = termsQuery.Where(t => t.Term.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

            if (category.HasValue)
                termsQuery = termsQuery.Where(t => t.Category == category.Value).ToList();

            foreach (var codeTerm in termsQuery.Take(100)) // Ограничиваем для производительности
            {
                var stat = new CodeUsageStatistic
                {
                    Term = codeTerm.Term,
                    Category = codeTerm.Category
                };

                // Находим все сообщения, содержащие этот термин
                var termMessages = messages
                    .Where(m => m.Dialog.Contains(codeTerm.Term, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!termMessages.Any())
                    continue;

                stat.TotalUsageCount = termMessages.Count;
                stat.UniqueCallsignsCount = termMessages
                    .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    .Distinct()
                    .Count();

                stat.UniqueAreasCount = termMessages
                    .Select(m => m.Area.Name)
                    .Distinct()
                    .Count();

                stat.FirstUsage = termMessages.Min(m => m.DateTime);
                stat.LastUsage = termMessages.Max(m => m.DateTime);

                // Статистика по позывным
                stat.UsageByCallsign = termMessages
                    .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    .GroupBy(callsign => callsign)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Статистика по зонам
                stat.UsageByArea = termMessages
                    .Select(m => m.Area.Name)
                    .GroupBy(area => area)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Использование по времени (по дням)
                stat.UsageOverTime = termMessages
                    .GroupBy(m => m.DateTime.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Среднее количество сообщений в день
                var daysWithUsage = stat.UsageOverTime.Keys.Count;
                stat.AverageMessagesPerDay = daysWithUsage > 0 ? (double)stat.TotalUsageCount / daysWithUsage : 0;

                // Расчет тренда (изменение частоты использования)
                stat.Trend = CalculateUsageTrend(stat.UsageOverTime);

                statistics.Add(stat);
            }

            return statistics
                .OrderByDescending(s => s.TotalUsageCount)
                .ThenByDescending(s => s.Trend)
                .ToList();
        }

        public async Task<Dictionary<string, List<string>>> FindTermAssociationsAsync(string term, int maxAssociations = 10)
        {
            var associations = new Dictionary<string, List<string>>();

            // Находим все сообщения, содержащие указанный термин
            var termMessages = await _context.Messages
                .Where(m => m.Dialog.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            if (!termMessages.Any())
                return associations;

            // Извлекаем слова из этих сообщений
            var allWords = new Dictionary<string, int>();

            foreach (var message in termMessages)
            {
                var words = ExtractWords(message.Dialog)
                    .Where(w => !string.Equals(w, term, StringComparison.OrdinalIgnoreCase))
                    .Where(w => w.Length > 2); // Игнорируем короткие слова

                foreach (var word in words)
                {
                    if (!allWords.ContainsKey(word))
                        allWords[word] = 0;
                    allWords[word]++;
                }
            }

            // Находим наиболее часто встречающиеся слова
            var frequentWords = allWords
                .Where(kv => kv.Value >= 3) // Минимум 3 совместных появления
                .OrderByDescending(kv => kv.Value)
                .Take(maxAssociations)
                .ToList();

            // Для каждого ассоциированного слова находим контексты
            foreach (var (associatedWord, count) in frequentWords)
            {
                var contexts = new List<string>();

                // Находим сообщения, где встречаются оба термина
                var jointMessages = termMessages
                    .Where(m => m.Dialog.Contains(associatedWord, StringComparison.OrdinalIgnoreCase))
                    .Take(5) // Берем первые 5 примеров
                    .ToList();

                foreach (var message in jointMessages)
                {
                    var context = ExtractContext(message.Dialog, term, associatedWord);
                    if (!string.IsNullOrEmpty(context))
                    {
                        contexts.Add(context);
                    }
                }

                associations[associatedWord] = contexts;
            }

            return associations;
        }

        public async Task<List<SlangPattern>> DetectSlangPatternsAsync(int minFrequency = 3)
        {
            var patterns = new List<SlangPattern>();

            // Получаем все сообщения
            var messages = await _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .ToListAsync();

            // Проверяем предопределенные шаблоны
            foreach (var (pattern, meaning) in _slangPatterns)
            {
                var regex = new Regex(pattern, RegexOptions.Compiled);
                var matches = new Dictionary<string, MatchInfo>();

                foreach (var message in messages)
                {
                    var messageMatches = regex.Matches(message.Dialog);
                    foreach (Match match in messageMatches)
                    {
                        var matchedText = match.Value;

                        if (!matches.ContainsKey(matchedText))
                        {
                            matches[matchedText] = new MatchInfo
                            {
                                Text = matchedText,
                                Count = 0,
                                Callsigns = new HashSet<string>(),
                                Messages = new List<Message>(),
                                FirstSeen = message.DateTime,
                                LastSeen = message.DateTime
                            };
                        }

                        var info = matches[matchedText];
                        info.Count++;

                        foreach (var callsign in message.MessageCallsigns.Select(mc => mc.Callsign.Name))
                        {
                            info.Callsigns.Add(callsign);
                        }

                        info.Messages.Add(message);

                        if (message.DateTime < info.FirstSeen)
                            info.FirstSeen = message.DateTime;
                        if (message.DateTime > info.LastSeen)
                            info.LastSeen = message.DateTime;
                    }
                }

                // Создаем паттерны для достаточно частых совпадений
                foreach (var match in matches.Values.Where(m => m.Count >= minFrequency))
                {
                    patterns.Add(new SlangPattern
                    {
                        Pattern = match.Text,
                        Meaning = meaning,
                        Examples = match.Messages
                            .Take(3)
                            .Select(m => Truncate(m.Dialog, 100))
                            .ToList(),
                        ExampleCount = match.Count,
                        Confidence = CalculatePatternConfidence(match),
                        AssociatedCallsigns = match.Callsigns.ToList(),
                        FirstObserved = match.FirstSeen,
                        LastObserved = match.LastSeen
                    });
                }
            }

            // Также ищем пользовательские паттерны (слова, которые часто используются вместе)
            var customPatterns = await DetectCustomSlangPatternsAsync(messages, minFrequency);
            patterns.AddRange(customPatterns);

            return patterns
                .OrderByDescending(p => p.Confidence)
                .ThenByDescending(p => p.ExampleCount)
                .ToList();
        }

        public async Task<List<SlangPattern>> FindCallsignSpecificSlangAsync(string callsign)
        {
            var specificPatterns = new List<SlangPattern>();

            // Получаем все сообщения этого позывного
            var callsignMessages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Include(mc => mc.Callsign)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message)
                .ToListAsync();

            if (!callsignMessages.Any())
                return specificPatterns;

            // Извлекаем слова из сообщений позывного
            var callsignWords = callsignMessages
                .SelectMany(m => ExtractWords(m.Dialog))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // Получаем слова из всех сообщений для сравнения
            var allMessages = await _context.Messages.ToListAsync();
            var allWords = allMessages
                .SelectMany(m => ExtractWords(m.Dialog))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // Находим слова, которые особенно характерны для этого позывного
            foreach (var (word, callsignCount) in callsignWords)
            {
                if (word.Length < 3 || callsignCount < 3)
                    continue;

                var totalCount = allWords.GetValueOrDefault(word, 0);
                if (totalCount == 0)
                    continue;

                var usageRatio = (double)callsignCount / totalCount;

                // Если позывной использует слово значительно чаще, чем в среднем
                if (usageRatio > 0.5 && callsignCount >= 3)
                {
                    var examples = callsignMessages
                        .Where(m => m.Dialog.Contains(word, StringComparison.OrdinalIgnoreCase))
                        .Take(3)
                        .Select(m => Truncate(m.Dialog, 100))
                        .ToList();

                    specificPatterns.Add(new SlangPattern
                    {
                        Pattern = word,
                        Meaning = $"Характерный термин для позывного {callsign} (использует в {usageRatio:P1} случаев)",
                        Examples = examples,
                        ExampleCount = callsignCount,
                        Confidence = usageRatio,
                        AssociatedCallsigns = new List<string> { callsign },
                        FirstObserved = callsignMessages
                            .Where(m => m.Dialog.Contains(word, StringComparison.OrdinalIgnoreCase))
                            .Min(m => m.DateTime),
                        LastObserved = callsignMessages
                            .Where(m => m.Dialog.Contains(word, StringComparison.OrdinalIgnoreCase))
                            .Max(m => m.DateTime)
                    });
                }
            }

            return specificPatterns
                .OrderByDescending(p => p.Confidence)
                .ThenByDescending(p => p.ExampleCount)
                .ToList();
        }

        public async Task<CallsignVocabularyProfile> GetCallsignVocabularyProfileAsync(string callsign)
        {
            var profile = new CallsignVocabularyProfile
            {
                Callsign = callsign
            };

            // Получаем все сообщения позывного
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message)
                .ToListAsync();

            if (!messages.Any())
                return profile;

            // Извлекаем все слова
            var allWords = messages
                .SelectMany(m => ExtractWords(m.Dialog))
                .ToList();

            profile.TotalWordsUsed = allWords.Count;
            profile.UniqueWordsCount = allWords.Distinct().Count();

            // Распределение по категориям
            var terms = await ExtractCodeTermsAsync();

            foreach (var word in allWords.Distinct())
            {
                var term = terms.FirstOrDefault(t => t.Term == word);
                if (term != null)
                {
                    if (!profile.CategoryDistribution.ContainsKey(term.Category))
                        profile.CategoryDistribution[term.Category] = 0;

                    profile.CategoryDistribution[term.Category]++;
                }
            }

            // Самые частые термины
            profile.MostFrequentTerms = allWords
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            // Отличительные термины
            profile.DistinctiveTerms = await FindDistinctiveTermsAsync(callsign, messages, terms);

            // Коэффициент разнообразия лексики
            profile.VocabularyRichness = profile.UniqueWordsCount > 0
                ? (double)profile.UniqueWordsCount / profile.TotalWordsUsed
                : 0;

            // Сходство с другими позывными
            profile.SimilarityScores = await CalculateSimilarityToOtherCallsignsAsync(callsign, messages);

            return profile;
        }

        public async Task<List<CallsignVocabularyProfile>> CompareCallsignVocabulariesAsync(List<string> callsigns)
        {
            var profiles = new List<CallsignVocabularyProfile>();

            foreach (var callsign in callsigns)
            {
                var profile = await GetCallsignVocabularyProfileAsync(callsign);
                profiles.Add(profile);
            }

            return profiles
                .OrderByDescending(p => p.VocabularyRichness)
                .ThenByDescending(p => p.UniqueWordsCount)
                .ToList();
        }

        public async Task<CodeSimilarityResult> CalculateVocabularySimilarityAsync(string callsign1, string callsign2)
        {
            var result = new CodeSimilarityResult
            {
                Callsign1 = callsign1,
                Callsign2 = callsign2
            };

            // Получаем профили обоих позывных
            var profile1 = await GetCallsignVocabularyProfileAsync(callsign1);
            var profile2 = await GetCallsignVocabularyProfileAsync(callsign2);

            if (profile1.TotalWordsUsed == 0 || profile2.TotalWordsUsed == 0)
            {
                result.SimilarityScore = 0;
                return result;
            }

            // Получаем словари обоих позывных
            var words1 = await GetCallsignWordsAsync(callsign1);
            var words2 = await GetCallsignWordsAsync(callsign2);

            // Общие термины
            var commonTerms = words1.Keys.Intersect(words2.Keys).ToList();
            result.CommonTerms = commonTerms
                .OrderByDescending(term => Math.Min(words1[term], words2[term]))
                .Take(20)
                .ToList();

            // Уникальные термины для каждого позывного
            result.UniqueToCallsign1 = words1.Keys
                .Except(words2.Keys)
                .OrderByDescending(term => words1[term])
                .Take(10)
                .ToList();

            result.UniqueToCallsign2 = words2.Keys
                .Except(words1.Keys)
                .OrderByDescending(term => words2[term])
                .Take(10)
                .ToList();

            // Сходство по категориям
            foreach (var category in Enum.GetValues(typeof(CodeTermCategory)).Cast<CodeTermCategory>())
            {
                var count1 = profile1.CategoryDistribution.GetValueOrDefault(category, 0);
                var count2 = profile2.CategoryDistribution.GetValueOrDefault(category, 0);

                var total = count1 + count2;
                if (total > 0)
                {
                    var similarity = 1.0 - Math.Abs(count1 - count2) / (double)total;
                    result.CategorySimilarity[category] = similarity;
                }
            }

            // Общий коэффициент сходства (Jaccard)
            var union = words1.Keys.Union(words2.Keys).Count();
            result.SimilarityScore = union > 0 ? (double)commonTerms.Count / union : 0;

            return result;
        }

        public async Task<List<List<string>>> ClusterCallsignsByTerminologyAsync(int minClusterSize = 2)
        {
            var clusters = new List<List<string>>();

            // Получаем все позывные
            var allCallsigns = await _context.Callsigns
                .Select(c => c.Name)
                .ToListAsync();

            // Создаем матрицу сходства
            var similarityMatrix = new Dictionary<string, Dictionary<string, double>>();

            foreach (var callsign1 in allCallsigns)
            {
                similarityMatrix[callsign1] = new Dictionary<string, double>();

                foreach (var callsign2 in allCallsigns)
                {
                    if (callsign1 == callsign2)
                    {
                        similarityMatrix[callsign1][callsign2] = 1.0;
                    }
                    else
                    {
                        var similarity = await CalculateVocabularySimilarityAsync(callsign1, callsign2);
                        similarityMatrix[callsign1][callsign2] = similarity.SimilarityScore;
                    }
                }
            }

            // Простая кластеризация по порогу сходства
            var processed = new HashSet<string>();
            var threshold = 0.3; // Порог сходства для объединения в кластер

            foreach (var callsign in allCallsigns)
            {
                if (processed.Contains(callsign))
                    continue;

                var cluster = new List<string> { callsign };
                processed.Add(callsign);

                // Ищем похожие позывные
                foreach (var other in allCallsigns.Where(c => !processed.Contains(c)))
                {
                    if (similarityMatrix[callsign][other] >= threshold)
                    {
                        cluster.Add(other);
                        processed.Add(other);
                    }
                }

                if (cluster.Count >= minClusterSize)
                {
                    clusters.Add(cluster);
                }
            }

            return clusters
                .OrderByDescending(c => c.Count)
                .ToList();
        }

        public async Task<List<CodeTerm>> DetectNewTermsAsync(DateTime since, double minDistinctiveness = 0.7)
        {
            var newTerms = new List<CodeTerm>();

            // Получаем термины, которые появились после указанной даты
            var allTerms = await ExtractCodeTermsAsync();

            foreach (var term in allTerms)
            {
                if (term.FirstSeen >= since && term.DistinctivenessScore >= minDistinctiveness)
                {
                    newTerms.Add(term);
                }
            }

            return newTerms
                .OrderByDescending(t => t.FirstSeen)
                .ThenByDescending(t => t.DistinctivenessScore)
                .ToList();
        }

        public async Task<Dictionary<string, double>> AnalyzeTermTrendsAsync(DateTime startDate, DateTime endDate)
        {
            var trends = new Dictionary<string, double>();

            // Разбиваем период на две равные части
            var middleDate = startDate.Add((endDate - startDate) / 2);

            // Получаем термины для первой и второй половины периода
            var firstHalfTerms = await ExtractCodeTermsAsync(startDate, middleDate);
            var secondHalfTerms = await ExtractCodeTermsAsync(middleDate, endDate);

            // Создаем словари для частот
            var firstHalfFreq = firstHalfTerms.ToDictionary(t => t.Term, t => t.FrequencyScore);
            var secondHalfFreq = secondHalfTerms.ToDictionary(t => t.Term, t => t.FrequencyScore);

            // Анализируем тренды для общих терминов
            var allTerms = firstHalfFreq.Keys.Union(secondHalfFreq.Keys);

            foreach (var term in allTerms)
            {
                var freq1 = firstHalfFreq.GetValueOrDefault(term, 0);
                var freq2 = secondHalfFreq.GetValueOrDefault(term, 0);

                if (freq1 > 0 && freq2 > 0)
                {
                    var trend = (freq2 - freq1) / freq1; // Относительное изменение
                    trends[term] = trend;
                }
                else if (freq2 > 0)
                {
                    trends[term] = 1.0; // Новый термин
                }
                else if (freq1 > 0)
                {
                    trends[term] = -1.0; // Исчезнувший термин
                }
            }

            return trends
                .OrderByDescending(kv => Math.Abs(kv.Value))
                .Take(50) // Ограничиваем количество
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task<Dictionary<string, string>> BuildGlossaryAsync(double minFrequency = 0.001)
        {
            var glossary = new Dictionary<string, string>();

            // Получаем все термины
            var terms = await ExtractCodeTermsAsync();

            // Фильтруем по частоте
            var filteredTerms = terms.Where(t => t.FrequencyScore >= minFrequency).ToList();

            // Предопределенные описания для известных категорий
            var categoryDescriptions = new Dictionary<CodeTermCategory, string>
            {
                [CodeTermCategory.Military] = "Военный термин",
                [CodeTermCategory.Slang] = "Сленговое выражение",
                [CodeTermCategory.CodeWord] = "Кодовое слово",
                [CodeTermCategory.Abbreviation] = "Аббревиатура",
                [CodeTermCategory.Technical] = "Технический термин",
                [CodeTermCategory.Location] = "Обозначение местоположения",
                [CodeTermCategory.Equipment] = "Термин, связанный с оборудованием",
                [CodeTermCategory.Operation] = "Оперативный термин",
                [CodeTermCategory.CallSignSpecific] = "Термин, специфичный для позывных",
                [CodeTermCategory.Urgency] = "Термин срочности"
            };

            foreach (var term in filteredTerms)
            {
                var description = term.Description;

                if (string.IsNullOrEmpty(description))
                {
                    description = categoryDescriptions.GetValueOrDefault(term.Category, "Неизвестный термин");

                    // Добавляем дополнительную информацию
                    if (term.TypicalContexts.Any())
                    {
                        description += $". Типичный контекст: {string.Join("; ", term.TypicalContexts.Take(2))}";
                    }
                }

                glossary[term.Term] = description;
            }

            return glossary
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        // Вспомогательные методы

        private List<string> ExtractWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Убираем пунктуацию и разбиваем на слова
            return text.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '[', ']', '\n', '\r', '\t' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2) // Игнорируем очень короткие слова
                .Where(w => !IsCommonWord(w)) // Игнорируем обычные слова
                .ToList();
        }

        private bool IsCommonWord(string word)
        {
            var commonWords = new HashSet<string>
            {
                "и", "в", "на", "с", "по", "для", "не", "что", "это", "как",
                "вот", "так", "но", "а", "или", "же", "то", "из", "у", "к",
                "да", "нет", "ой", "ах", "эх", "ну", "все", "еще", "уже",
                "там", "здесь", "тут", "где", "когда", "какой", "какая", "какое",
                "кто", "чего", "чем", "мне", "меня", "мной", "тебе", "тебя"
            };

            return commonWords.Contains(word);
        }

        private CodeTermCategory DetermineTermCategory(string word)
        {
            // Проверяем предопределенные словари
            foreach (var category in _predefinedTerms.Keys)
            {
                if (_predefinedTerms[category].Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    return category;
                }
            }

            // Проверяем паттерны
            foreach (var (pattern, _) in _slangPatterns)
            {
                if (Regex.IsMatch(word, pattern))
                {
                    return pattern.Contains("[А-Я]") ? CodeTermCategory.CodeWord : CodeTermCategory.Slang;
                }
            }

            // Проверяем по морфологическим признакам
            if (word.All(c => char.IsUpper(c)) && word.Length >= 2 && word.Length <= 4)
                return CodeTermCategory.Abbreviation;

            if (word.Contains('-') || word.Contains('/'))
                return CodeTermCategory.Slang;

            if (char.IsDigit(word[0]) && word.Any(char.IsLetter))
                return CodeTermCategory.CodeWord;

            return CodeTermCategory.Unknown;
        }

        private double CalculateDistinctiveness(WordStatistics stats, IEnumerable<WordStatistics> allStats)
        {
            // Рассчитываем, насколько термин отличителен
            // Основные факторы:
            // 1. Количество уникальных позывных, использующих термин
            // 2. Количество уникальных зон
            // 3. Частота по сравнению с другими терминами

            var maxCallsigns = allStats.Max(s => s.Callsigns.Count);
            var maxAreas = allStats.Max(s => s.Areas.Count);
            var maxFrequency = allStats.Max(s => s.Frequency);

            var callsignScore = maxCallsigns > 0 ? (double)stats.Callsigns.Count / maxCallsigns : 0;
            var areaScore = maxAreas > 0 ? (double)stats.Areas.Count / maxAreas : 0;
            var frequencyScore = maxFrequency > 0 ? (double)stats.Frequency / maxFrequency : 0;

            // Весовые коэффициенты
            return callsignScore * 0.4 + areaScore * 0.3 + frequencyScore * 0.3;
        }

        private async Task<List<string>> ExtractTypicalContextsAsync(string term, List<Message> messages)
        {
            var contexts = new List<string>();

            // Находим сообщения с термином
            var termMessages = messages
                .Where(m => m.Dialog.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(10) // Ограничиваем количество
                .ToList();

            foreach (var message in termMessages)
            {
                // Извлекаем контекст вокруг термина
                var index = message.Dialog.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var start = Math.Max(0, index - 30);
                    var end = Math.Min(message.Dialog.Length, index + term.Length + 30);
                    var context = message.Dialog.Substring(start, end - start);

                    if (start > 0) context = "..." + context;
                    if (end < message.Dialog.Length) context = context + "...";

                    contexts.Add(context);
                }
            }

            return contexts.Distinct().Take(3).ToList();
        }

        private double CalculateUsageTrend(Dictionary<DateTime, int> usageOverTime)
        {
            if (usageOverTime.Count < 2)
                return 0;

            var dates = usageOverTime.Keys.OrderBy(d => d).ToList();
            var half = dates.Count / 2;

            var firstHalf = dates.Take(half).Sum(d => usageOverTime[d]);
            var secondHalf = dates.Skip(half).Sum(d => usageOverTime[d]);

            var firstHalfAvg = (double)firstHalf / half;
            var secondHalfAvg = (double)secondHalf / (dates.Count - half);

            if (firstHalfAvg == 0)
                return secondHalfAvg > 0 ? 1 : 0;

            return (secondHalfAvg - firstHalfAvg) / firstHalfAvg;
        }

        private string ExtractContext(string text, string term1, string term2)
        {
            var index1 = text.IndexOf(term1, StringComparison.OrdinalIgnoreCase);
            var index2 = text.IndexOf(term2, StringComparison.OrdinalIgnoreCase);

            if (index1 < 0 || index2 < 0)
                return string.Empty;

            var start = Math.Min(index1, index2);
            var end = Math.Max(index1 + term1.Length, index2 + term2.Length);

            // Расширяем контекст
            start = Math.Max(0, start - 20);
            end = Math.Min(text.Length, end + 20);

            var context = text.Substring(start, end - start);

            if (start > 0) context = "..." + context;
            if (end < text.Length) context = context + "...";

            return context;
        }

        private async Task<List<SlangPattern>> DetectCustomSlangPatternsAsync(List<Message> messages, int minFrequency)
        {
            var patterns = new List<SlangPattern>();

            // Ищем часто встречающиеся словосочетания (2-3 слова)
            var phraseFreq = new Dictionary<string, PhraseInfo>();

            foreach (var message in messages)
            {
                var words = ExtractWords(message.Dialog);

                // Извлекаем словосочетания из 2-3 слов
                for (int i = 0; i <= words.Count - 2; i++)
                {
                    for (int length = 2; length <= 3 && i + length <= words.Count; length++)
                    {
                        var phrase = string.Join(" ", words.Skip(i).Take(length));

                        if (!phraseFreq.ContainsKey(phrase))
                        {
                            phraseFreq[phrase] = new PhraseInfo
                            {
                                Phrase = phrase,
                                Count = 0,
                                Callsigns = new HashSet<string>(),
                                Messages = new List<Message>()
                            };
                        }

                        var info = phraseFreq[phrase];
                        info.Count++;

                        foreach (var callsign in message.MessageCallsigns.Select(mc => mc.Callsign.Name))
                        {
                            info.Callsigns.Add(callsign);
                        }

                        info.Messages.Add(message);
                    }
                }
            }

            // Фильтруем по частоте и создаем паттерны
            foreach (var info in phraseFreq.Values.Where(i => i.Count >= minFrequency))
            {
                // Проверяем, не является ли это обычным словосочетанием
                if (IsCommonPhrase(info.Phrase))
                    continue;

                patterns.Add(new SlangPattern
                {
                    Pattern = info.Phrase,
                    Meaning = "Часто используемое словосочетание",
                    Examples = info.Messages
                        .Take(3)
                        .Select(m => Truncate(m.Dialog, 100))
                        .ToList(),
                    ExampleCount = info.Count,
                    Confidence = Math.Min(1.0, (double)info.Count / messages.Count * 100),
                    AssociatedCallsigns = info.Callsigns.ToList(),
                    FirstObserved = info.Messages.Min(m => m.DateTime),
                    LastObserved = info.Messages.Max(m => m.DateTime)
                });
            }

            return patterns;
        }

        private bool IsCommonPhrase(string phrase)
        {
            var commonPhrases = new[]
            {
                "на месте", "все в порядке", "прием конец", "добрый день",
                "доброе утро", "добрый вечер", "как слышно", "повторите пожалуйста"
            };

            return commonPhrases.Contains(phrase.ToLower());
        }

        private double CalculatePatternConfidence(MatchInfo match)
        {
            // Уверенность в паттерне основана на:
            // 1. Частоте использования
            // 2. Количестве уникальных позывных
            // 3. Продолжительности использования

            var frequencyScore = Math.Min(1.0, (double)match.Count / 10);
            var callsignScore = Math.Min(1.0, (double)match.Callsigns.Count / 5);
            var durationScore = Math.Min(1.0, (match.LastSeen - match.FirstSeen).TotalDays / 30);

            return frequencyScore * 0.5 + callsignScore * 0.3 + durationScore * 0.2;
        }

        private async Task<List<string>> FindDistinctiveTermsAsync(string callsign, List<Message> callsignMessages, List<CodeTerm> allTerms)
        {
            var distinctiveTerms = new List<string>();

            // Получаем словарь этого позывного
            var callsignWords = callsignMessages
                .SelectMany(m => ExtractWords(m.Dialog))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // Получаем словарь всех сообщений
            var allMessages = await _context.Messages.ToListAsync();
            var allWords = allMessages
                .SelectMany(m => ExtractWords(m.Dialog))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var (word, callsignCount) in callsignWords)
            {
                if (word.Length < 3 || callsignCount < 3)
                    continue;

                var totalCount = allWords.GetValueOrDefault(word, 0);
                if (totalCount == 0)
                    continue;

                // Рассчитываем относительную частоту
                var callsignFrequency = (double)callsignCount / callsignMessages.Count;
                var globalFrequency = (double)totalCount / allMessages.Count;

                // Если позывной использует термин значительно чаще
                if (callsignFrequency > globalFrequency * 3)
                {
                    distinctiveTerms.Add(word);
                }
            }

            return distinctiveTerms
                .OrderByDescending(term => callsignWords[term])
                .Take(10)
                .ToList();
        }

        private async Task<Dictionary<string, double>> CalculateSimilarityToOtherCallsignsAsync(string targetCallsign, List<Message> targetMessages)
        {
            var similarities = new Dictionary<string, double>();

            // Получаем все позывные
            var allCallsigns = await _context.Callsigns
                .Where(c => c.Name != targetCallsign)
                .Select(c => c.Name)
                .ToListAsync();

            // Получаем словарь целевого позывного
            var targetWords = targetMessages
                .SelectMany(m => ExtractWords(m.Dialog))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var otherCallsign in allCallsigns.Take(20)) // Ограничиваем для производительности
            {
                // Получаем сообщения другого позывного
                var otherMessages = await _context.MessageCallsigns
                    .Include(mc => mc.Message)
                    .Where(mc => mc.Callsign.Name == otherCallsign)
                    .Select(mc => mc.Message)
                    .ToListAsync();

                if (!otherMessages.Any())
                    continue;

                // Получаем словарь другого позывного
                var otherWords = otherMessages
                    .SelectMany(m => ExtractWords(m.Dialog))
                    .GroupBy(w => w)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Рассчитываем коэффициент Жаккара
                var intersection = targetWords.Keys.Intersect(otherWords.Keys).Count();
                var union = targetWords.Keys.Union(otherWords.Keys).Count();

                if (union > 0)
                {
                    similarities[otherCallsign] = (double)intersection / union;
                }
            }

            return similarities
                .OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private async Task<Dictionary<string, int>> GetCallsignWordsAsync(string callsign)
        {
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message)
                .ToListAsync();

            return messages
                .SelectMany(m => ExtractWords(m.Dialog))
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        // Вспомогательные классы
        private class WordStatistics
        {
            public string Word { get; set; } = null!;
            public int Frequency { get; set; }
            public HashSet<string> Callsigns { get; set; } = new();
            public HashSet<string> Areas { get; set; } = new();
            public HashSet<long> Messages { get; set; } = new();
            public DateTime FirstSeen { get; set; } = DateTime.MaxValue;
            public DateTime LastSeen { get; set; } = DateTime.MinValue;
        }

        private class MatchInfo
        {
            public string Text { get; set; } = null!;
            public int Count { get; set; }
            public HashSet<string> Callsigns { get; set; } = new();
            public List<Message> Messages { get; set; } = new();
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
        }

        private class PhraseInfo
        {
            public string Phrase { get; set; } = null!;
            public int Count { get; set; }
            public HashSet<string> Callsigns { get; set; } = new();
            public List<Message> Messages { get; set; } = new();
        }
    }
}