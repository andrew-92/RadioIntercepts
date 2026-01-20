// Application/Services/SemanticSearchService.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Services
{
    public interface ISemanticSearchService
    {
        Task<List<SemanticSearchResult>> SearchAsync(SemanticSearchQuery query);
        Task<List<SemanticSearchResult>> SearchByExampleAsync(SearchByExampleRequest request);
        Task<List<KeywordAnalysis>> AnalyzeKeywordsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<MessageCategory>> GetMessageCategoriesAsync();
        Task<Dictionary<MessageType, List<string>>> ExtractTypicalPhrasesAsync(MessageType type, int topN = 10);
        Task<List<Message>> FindSimilarMessagesAsync(long messageId, int maxResults = 10);
        Task<List<MessageCluster>> ClusterMessagesByContentAsync(int numClusters = 5);
        Task<Dictionary<string, double>> CalculateTermFrequencyAsync(string term);
    }

    public class SemanticSearchService : ISemanticSearchService
    {
        private readonly AppDbContext _context;
        private readonly IDialogPatternAnalyzer _dialogAnalyzer;

        // Стоп-слова для русского языка
        private static readonly HashSet<string> StopWords = new()
        {
            "и", "в", "на", "с", "по", "для", "не", "что", "это", "как",
            "вот", "так", "но", "а", "или", "же", "то", "из", "у", "к",
            "да", "нет", "ой", "ах", "эх", "ну", "все", "еще", "уже",
            "там", "здесь", "тут", "где", "когда", "какой", "какая", "какое",
            "кто", "чего", "чем", "мне", "меня", "мной", "тебе", "тебя",
            "тобой", "ему", "его", "им", "ей", "ее", "ею", "нам", "нас",
            "нами", "вам", "вас", "вами", "им", "их", "ими", "свой", "своя",
            "свое", "свои", "наш", "наша", "наше", "наши", "ваш", "ваша",
            "ваше", "ваши", "ихний", "ихняя", "ихнее", "ихние"
        };

        // Специальные термины (можно расширять)
        private static readonly Dictionary<string, double> SpecialTerms = new()
        {
            ["координаты"] = 2.0,
            ["пеленг"] = 2.0,
            ["азимут"] = 2.0,
            ["дистанция"] = 2.0,
            ["цель"] = 1.5,
            ["объект"] = 1.5,
            ["техника"] = 1.5,
            ["личный"] = 1.5,
            ["состав"] = 1.5,
            ["боеприпасы"] = 2.0,
            ["топливо"] = 2.0,
            ["ранен"] = 2.5,
            ["убит"] = 2.5,
            ["помощь"] = 2.0,
            ["поддержка"] = 2.0,
            ["атака"] = 2.5,
            ["отход"] = 2.5,
            ["занял"] = 2.0,
            ["оставил"] = 2.0,
            ["вижу"] = 1.5,
            ["слышу"] = 1.5,
            ["обнаружил"] = 2.0
        };

        public SemanticSearchService(AppDbContext context, IDialogPatternAnalyzer dialogAnalyzer)
        {
            _context = context;
            _dialogAnalyzer = dialogAnalyzer;
        }

        public async Task<List<SemanticSearchResult>> SearchAsync(SemanticSearchQuery query)
        {
            var results = new List<SemanticSearchResult>();

            // Базовый запрос с фильтрами
            var dbQuery = BuildFilteredQuery(query);
            var messages = await dbQuery.ToListAsync();

            if (!messages.Any())
                return results;

            // Токенизация запроса
            var queryTokens = TokenizeText(query.Query);
            var queryKeywords = ExtractKeywords(query.Query);

            // Вычисляем IDF для всех терминов в корпусе
            var idfCache = await CalculateIDFCacheAsync();

            foreach (var message in messages)
            {
                var similarity = CalculateSimilarity(
                    queryTokens,
                    TokenizeText(message.Dialog),
                    queryKeywords,
                    idfCache);

                if (similarity >= query.MinSimilarity)
                {
                    var matchedKeywords = FindMatchedKeywords(queryKeywords, message.Dialog);
                    var snippet = GenerateSnippet(message.Dialog, queryKeywords);

                    results.Add(new SemanticSearchResult
                    {
                        Message = message,
                        SimilarityScore = similarity,
                        MatchedKeywords = matchedKeywords,
                        DetectedType = _dialogAnalyzer.ClassifySingleMessage(message.Dialog),
                        KeywordWeights = CalculateKeywordWeights(queryKeywords, message.Dialog, idfCache),
                        Snippet = snippet
                    });
                }
            }

            return results
                .OrderByDescending(r => r.SimilarityScore)
                .ThenByDescending(r => r.Message.DateTime)
                .Take(query.MaxResults)
                .ToList();
        }

        public async Task<List<SemanticSearchResult>> SearchByExampleAsync(SearchByExampleRequest request)
        {
            // Находим похожие сообщения на основе примера
            var query = new SemanticSearchQuery
            {
                Query = request.ExampleText,
                MinSimilarity = 0.4,
                MaxResults = request.MaxSimilarExamples * 2
            };

            var similarResults = await SearchAsync(query);

            if (request.IncludeOpposite)
            {
                // Находим противоположные по смыслу сообщения
                var oppositeKeywords = GetOppositeKeywords(request.ExampleText);
                if (oppositeKeywords.Any())
                {
                    var oppositeQuery = new SemanticSearchQuery
                    {
                        Query = string.Join(" ", oppositeKeywords),
                        MinSimilarity = 0.3,
                        MaxResults = request.MaxSimilarExamples
                    };

                    var oppositeResults = await SearchAsync(oppositeQuery);
                    similarResults.AddRange(oppositeResults);
                }
            }

            return similarResults
                .OrderByDescending(r => r.SimilarityScore)
                .Take(request.MaxSimilarExamples)
                .ToList();
        }

        public async Task<List<KeywordAnalysis>> AnalyzeKeywordsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Messages.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.DateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.DateTime <= endDate.Value);

            var messages = await query
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .ToListAsync();

            var allKeywords = new Dictionary<string, KeywordData>();
            var totalDocuments = messages.Count;

            // Собираем статистику по всем ключевым словам
            foreach (var message in messages)
            {
                var keywords = ExtractKeywords(message.Dialog, 20);
                var uniqueKeywords = new HashSet<string>(keywords.Select(k => k.Word));

                foreach (var keyword in keywords)
                {
                    if (!allKeywords.ContainsKey(keyword.Word))
                    {
                        allKeywords[keyword.Word] = new KeywordData
                        {
                            Word = keyword.Word,
                            Frequency = 0,
                            DocumentFrequency = 0,
                            RelatedCallsigns = new HashSet<string>(),
                            RelatedAreas = new HashSet<string>(),
                            FirstSeen = message.DateTime,
                            LastSeen = message.DateTime
                        };
                    }

                    var data = allKeywords[keyword.Word];
                    data.Frequency += keyword.Frequency;
                    data.DocumentFrequency++;

                    // Собираем связанные данные
                    foreach (var callsign in message.MessageCallsigns.Select(mc => mc.Callsign.Name))
                    {
                        data.RelatedCallsigns.Add(callsign);
                    }

                    data.RelatedAreas.Add(message.Area.Name);

                    if (message.DateTime < data.FirstSeen)
                        data.FirstSeen = message.DateTime;
                    if (message.DateTime > data.LastSeen)
                        data.LastSeen = message.DateTime;
                }
            }

            // Рассчитываем TF-IDF и создаем результат
            var results = new List<KeywordAnalysis>();

            foreach (var kvp in allKeywords)
            {
                var data = kvp.Value;
                var tf = (double)data.Frequency / allKeywords.Values.Sum(k => k.Frequency);
                var idf = Math.Log((double)totalDocuments / (data.DocumentFrequency + 1));
                var tfidf = tf * idf;

                // Увеличиваем вес специальных терминов
                if (SpecialTerms.ContainsKey(data.Word))
                {
                    tfidf *= SpecialTerms[data.Word];
                }

                results.Add(new KeywordAnalysis
                {
                    Keyword = data.Word,
                    Frequency = data.Frequency,
                    TFIDF = tfidf,
                    RelatedCallsigns = data.RelatedCallsigns.Take(10).ToList(),
                    RelatedAreas = data.RelatedAreas.Take(5).ToList(),
                    FirstSeen = data.FirstSeen,
                    LastSeen = data.LastSeen
                });
            }

            return results
                .OrderByDescending(k => k.TFIDF)
                .ThenByDescending(k => k.Frequency)
                .Take(100)
                .ToList();
        }

        public async Task<List<MessageCategory>> GetMessageCategoriesAsync()
        {
            var categories = new List<MessageCategory>();

            // Определяем категории на основе типов сообщений
            foreach (MessageType type in Enum.GetValues(typeof(MessageType)))
            {
                if (type == MessageType.Unknown)
                    continue;

                var typicalPhrases = await ExtractTypicalPhrasesAsync(type, 15);

                var category = new MessageCategory
                {
                    Name = type.ToString(),
                    Description = GetCategoryDescription(type),
                    Keywords = typicalPhrases[type].Take(10).ToList(),
                    ExamplePhrases = typicalPhrases[type].Take(5).ToList(),
                    MessageCount = await _context.Messages
                        .CountAsync(m => _dialogAnalyzer.ClassifySingleMessage(m.Dialog) == type)
                };

                categories.Add(category);
            }

            // Дополнительные тематические категории
            categories.AddRange(new[]
            {
                new MessageCategory
                {
                    Name = "Координаты_и_местоположение",
                    Description = "Сообщения с координатами, пеленгами, указанием местоположения",
                    Keywords = new List<string> { "координаты", "пеленг", "азимут", "дистанция", "местоположение", "точка", "сектор", "квадрат" },
                    ExamplePhrases = new List<string> { "координаты цели", "пеленг 270", "азимут 45 градусов" },
                    MessageCount = await CountMessagesWithKeywordsAsync(new[] { "координаты", "пеленг", "азимут", "дистанция" })
                },
                new MessageCategory
                {
                    Name = "Техника_и_снаряжение",
                    Description = "Сообщения о технике, вооружении, снаряжении",
                    Keywords = new List<string> { "техника", "танк", "бмп", "бтр", "артиллерия", "вооружение", "снаряжение", "боеприпасы", "топливо" },
                    ExamplePhrases = new List<string> { "техника на позиции", "боеприпасы на исходе", "требуется топливо" },
                    MessageCount = await CountMessagesWithKeywordsAsync(new[] { "техника", "танк", "бмп", "боеприпасы", "топливо" })
                },
                new MessageCategory
                {
                    Name = "Потери_и_ранения",
                    Description = "Сообщения о потерях, ранениях, медицинской помощи",
                    Keywords = new List<string> { "ранен", "убит", "потери", "медик", "помощь", "кровь", "перевязка", "эвакуация" },
                    ExamplePhrases = new List<string> { "двое раненых", "требуется медик", "эвакуировать раненого" },
                    MessageCount = await CountMessagesWithKeywordsAsync(new[] { "ранен", "убит", "потери", "медик", "помощь" })
                },
                new MessageCategory
                {
                    Name = "Тактические_действия",
                    Description = "Сообщения о тактических перемещениях и действиях",
                    Keywords = new List<string> { "атака", "отход", "наступление", "оборона", "занял", "оставил", "выдвигаюсь", "отступаю" },
                    ExamplePhrases = new List<string> { "переходим в атаку", "отход на вторую линию", "занял оборону" },
                    MessageCount = await CountMessagesWithKeywordsAsync(new[] { "атака", "отход", "наступление", "оборона", "занял" })
                }
            });

            return categories.OrderByDescending(c => c.MessageCount).ToList();
        }

        public async Task<Dictionary<MessageType, List<string>>> ExtractTypicalPhrasesAsync(MessageType type, int topN = 10)
        {
            var result = new Dictionary<MessageType, List<string>>();

            // Получаем все сообщения данного типа
            var messages = await _context.Messages
                .Where(m => _dialogAnalyzer.ClassifySingleMessage(m.Dialog) == type)
                .Select(m => m.Dialog)
                .ToListAsync();

            if (!messages.Any())
            {
                result[type] = new List<string>();
                return result;
            }

            // Извлекаем ключевые фразы
            var phraseFreq = new Dictionary<string, int>();

            foreach (var message in messages)
            {
                var phrases = ExtractPhrases(message, 2, 4); // Фразы из 2-4 слов
                foreach (var phrase in phrases)
                {
                    if (!phraseFreq.ContainsKey(phrase))
                        phraseFreq[phrase] = 0;
                    phraseFreq[phrase]++;
                }
            }

            result[type] = phraseFreq
                .OrderByDescending(p => p.Value)
                .Take(topN)
                .Select(p => p.Key)
                .ToList();

            return result;
        }

        public async Task<List<Message>> FindSimilarMessagesAsync(long messageId, int maxResults = 10)
        {
            var sourceMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (sourceMessage == null)
                return new List<Message>();

            var request = new SearchByExampleRequest
            {
                ExampleText = sourceMessage.Dialog,
                MaxSimilarExamples = maxResults
            };

            var results = await SearchByExampleAsync(request);
            return results.Select(r => r.Message).ToList();
        }

        public async Task<List<MessageCluster>> ClusterMessagesByContentAsync(int numClusters = 5)
        {
            // Упрощенная кластеризация по ключевым словам
            var messages = await _context.Messages
                .Take(1000) // Ограничиваем для производительности
                .ToListAsync();

            var clusters = new List<MessageCluster>();

            // Извлекаем ключевые слова для каждого сообщения
            var messageKeywords = new Dictionary<long, List<string>>();
            foreach (var message in messages)
            {
                var keywords = ExtractKeywords(message.Dialog, 5)
                    .Select(k => k.Word)
                    .ToList();
                messageKeywords[message.Id] = keywords;
            }

            // Простая кластеризация по пересечению ключевых слов
            var processed = new HashSet<long>();

            foreach (var message in messages)
            {
                if (processed.Contains(message.Id))
                    continue;

                var clusterKeywords = messageKeywords[message.Id].ToHashSet();
                var clusterMessages = new List<Message> { message };
                processed.Add(message.Id);

                // Ищем сообщения с похожими ключевыми словами
                foreach (var other in messages.Where(m => !processed.Contains(m.Id)))
                {
                    var otherKeywords = messageKeywords[other.Id];
                    var intersection = clusterKeywords.Intersect(otherKeywords).Count();

                    if (intersection >= 2) // Минимум 2 общих ключевых слова
                    {
                        clusterMessages.Add(other);
                        processed.Add(other.Id);

                        // Обновляем ключевые слова кластера
                        foreach (var kw in otherKeywords)
                        {
                            clusterKeywords.Add(kw);
                        }
                    }
                }

                if (clusterMessages.Count >= 3) // Минимум 3 сообщения в кластере
                {
                    clusters.Add(new MessageCluster
                    {
                        Id = clusters.Count + 1,
                        Keywords = clusterKeywords.Take(10).ToList(),
                        Messages = clusterMessages,
                        Size = clusterMessages.Count,
                        AverageSimilarity = CalculateClusterCohesion(clusterMessages, messageKeywords)
                    });
                }

                if (clusters.Count >= numClusters)
                    break;
            }

            return clusters.OrderByDescending(c => c.Size).ToList();
        }

        public async Task<Dictionary<string, double>> CalculateTermFrequencyAsync(string term)
        {
            var result = new Dictionary<string, double>();

            // Частота термина по дням
            var dailyFreq = await _context.Messages
                .Where(m => m.Dialog.Contains(term))
                .GroupBy(m => m.DateTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date.ToString("yyyy-MM-dd"), x => (double)x.Count);

            // Частота термина по зонам
            var areaFreq = await _context.Messages
                .Include(m => m.Area)
                .Where(m => m.Dialog.Contains(term))
                .GroupBy(m => m.Area.Name)
                .Select(g => new { Area = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Area, x => (double)x.Count);

            // Частота термина по позывным
            var callsignFreq = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Include(mc => mc.Callsign)
                .Where(mc => mc.Message.Dialog.Contains(term))
                .GroupBy(mc => mc.Callsign.Name)
                .Select(g => new { Callsign = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Callsign, x => (double)x.Count);

            // Объединяем результаты
            result["total"] = dailyFreq.Values.Sum();
            result["avg_per_day"] = dailyFreq.Values.Any() ? dailyFreq.Values.Average() : 0;

            if (areaFreq.Any())
            {
                result["most_common_area"] = areaFreq.OrderByDescending(kv => kv.Value).First().Value;
            }

            if (callsignFreq.Any())
            {
                result["most_common_callsign"] = callsignFreq.OrderByDescending(kv => kv.Value).First().Value;
            }

            return result;
        }

        // Вспомогательные методы

        private IQueryable<Message> BuildFilteredQuery(SemanticSearchQuery query)
        {
            IQueryable<Message> dbQuery = _context.Messages
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .AsQueryable();

            if (query.DateFrom.HasValue)
                dbQuery = dbQuery.Where(m => m.DateTime >= query.DateFrom.Value);
            if (query.DateTo.HasValue)
                dbQuery = dbQuery.Where(m => m.DateTime <= query.DateTo.Value);
            if (!string.IsNullOrEmpty(query.Area))
                dbQuery = dbQuery.Where(m => m.Area.Name == query.Area);
            if (!string.IsNullOrEmpty(query.Frequency))
                dbQuery = dbQuery.Where(m => m.Frequency.Value == query.Frequency);
            if (query.Callsigns != null && query.Callsigns.Any())
                dbQuery = dbQuery.Where(m => m.MessageCallsigns.Any(mc => query.Callsigns.Contains(mc.Callsign.Name)));
            if (query.MessageType.HasValue)
            {
                // Фильтрация по типу сообщения требует вычисления для каждого сообщения
                // Это может быть медленно для больших наборов данных
            }

            return dbQuery;
        }

        private List<string> TokenizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            return text.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '(', ')', '[', ']', '\n', '\r', '\t' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !StopWords.Contains(w))
                .ToList();
        }

        private List<KeywordFrequency> ExtractKeywords(string text, int maxKeywords = 10)
        {
            var tokens = TokenizeText(text);
            var frequencies = tokens
                .GroupBy(w => w)
                .Select(g => new KeywordFrequency
                {
                    Word = g.Key,
                    Frequency = g.Count(),
                    Weight = SpecialTerms.ContainsKey(g.Key) ? SpecialTerms[g.Key] : 1.0
                })
                .OrderByDescending(k => k.Weight * k.Frequency)
                .Take(maxKeywords)
                .ToList();

            return frequencies;
        }

        private async Task<Dictionary<string, double>> CalculateIDFCacheAsync()
        {
            var totalDocuments = await _context.Messages.CountAsync();
            var termDocumentCount = new Dictionary<string, int>();

            // Берем выборку сообщений для расчета (можно увеличить для точности)
            var sampleMessages = await _context.Messages
                .Take(5000)
                .Select(m => m.Dialog)
                .ToListAsync();

            foreach (var message in sampleMessages)
            {
                var uniqueTokens = new HashSet<string>(TokenizeText(message));
                foreach (var token in uniqueTokens)
                {
                    if (!termDocumentCount.ContainsKey(token))
                        termDocumentCount[token] = 0;
                    termDocumentCount[token]++;
                }
            }

            return termDocumentCount.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Log((double)totalDocuments / (kvp.Value + 1))
            );
        }

        private double CalculateSimilarity(
            List<string> queryTokens,
            List<string> documentTokens,
            List<KeywordFrequency> queryKeywords,
            Dictionary<string, double> idfCache)
        {
            if (!queryTokens.Any() || !documentTokens.Any())
                return 0.0;

            // Косинусное сходство с TF-IDF
            var queryVector = new Dictionary<string, double>();
            var docVector = new Dictionary<string, double>();

            // TF для запроса
            foreach (var token in queryTokens)
            {
                if (!queryVector.ContainsKey(token))
                    queryVector[token] = 0;
                queryVector[token] += 1.0;
            }

            // TF для документа
            foreach (var token in documentTokens)
            {
                if (!docVector.ContainsKey(token))
                    docVector[token] = 0;
                docVector[token] += 1.0;
            }

            // Применяем IDF и нормализуем
            double queryNorm = 0;
            double docNorm = 0;
            double dotProduct = 0;

            var allTerms = queryVector.Keys.Union(docVector.Keys);

            foreach (var term in allTerms)
            {
                double queryTf = queryVector.ContainsKey(term) ? queryVector[term] : 0;
                double docTf = docVector.ContainsKey(term) ? docVector[term] : 0;

                double idf = idfCache.ContainsKey(term) ? idfCache[term] : Math.Log(1000); // Дефолтное значение

                double queryWeight = queryTf * idf;
                double docWeight = docTf * idf;

                // Учитываем вес ключевых слов из запроса
                var keyword = queryKeywords.FirstOrDefault(k => k.Word == term);
                if (keyword != null)
                {
                    queryWeight *= keyword.Weight;
                    docWeight *= keyword.Weight;
                }

                queryNorm += queryWeight * queryWeight;
                docNorm += docWeight * docWeight;
                dotProduct += queryWeight * docWeight;
            }

            if (queryNorm == 0 || docNorm == 0)
                return 0.0;

            return dotProduct / (Math.Sqrt(queryNorm) * Math.Sqrt(docNorm));
        }

        private List<string> FindMatchedKeywords(List<KeywordFrequency> queryKeywords, string document)
        {
            var matched = new List<string>();
            var docLower = document.ToLower();

            foreach (var keyword in queryKeywords)
            {
                if (docLower.Contains(keyword.Word))
                {
                    matched.Add(keyword.Word);
                }
            }

            return matched;
        }

        private Dictionary<string, double> CalculateKeywordWeights(
            List<KeywordFrequency> queryKeywords,
            string document,
            Dictionary<string, double> idfCache)
        {
            var weights = new Dictionary<string, double>();
            var tokens = TokenizeText(document);
            var tokenFreq = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

            foreach (var keyword in queryKeywords)
            {
                if (tokenFreq.ContainsKey(keyword.Word))
                {
                    double tf = tokenFreq[keyword.Word];
                    double idf = idfCache.ContainsKey(keyword.Word) ? idfCache[keyword.Word] : 1.0;
                    weights[keyword.Word] = tf * idf * keyword.Weight;
                }
            }

            return weights;
        }

        private string GenerateSnippet(string document, List<KeywordFrequency> keywords, int length = 200)
        {
            if (string.IsNullOrEmpty(document))
                return "";

            if (document.Length <= length)
                return document;

            // Ищем позицию первого ключевого слова
            foreach (var keyword in keywords.OrderByDescending(k => k.Weight))
            {
                int index = document.ToLower().IndexOf(keyword.Word);
                if (index >= 0)
                {
                    int start = Math.Max(0, index - length / 2);
                    int end = Math.Min(document.Length, start + length);
                    start = Math.Max(0, end - length);

                    var snippet = document.Substring(start, end - start);
                    if (start > 0)
                        snippet = "..." + snippet;
                    if (end < document.Length)
                        snippet = snippet + "...";

                    return snippet;
                }
            }

            // Если ключевых слов нет, берем начало
            return document.Length <= length ? document : document.Substring(0, length) + "...";
        }

        private List<string> GetOppositeKeywords(string text)
        {
            var opposites = new Dictionary<string, List<string>>
            {
                ["атака"] = new List<string> { "отход", "отступление", "отойти" },
                ["отход"] = new List<string> { "атака", "наступление", "вперед" },
                ["ранен"] = new List<string> { "здоров", "готов", "боеспособен" },
                ["убит"] = new List<string> { "жив", "цел", "невредим" },
                ["помощь"] = new List<string> { "отказ", "отмена", "не нужно" },
                ["требуется"] = new List<string> { "достаточно", "есть", "имеется" },
                ["вижу"] = new List<string> { "не вижу", "пусто", "чисто" },
                ["обнаружил"] = new List<string> { "пропал", "исчез", "скрылся" }
            };

            var result = new List<string>();
            var tokens = TokenizeText(text);

            foreach (var token in tokens)
            {
                if (opposites.ContainsKey(token))
                {
                    result.AddRange(opposites[token]);
                }
            }

            return result.Distinct().ToList();
        }

        private List<string> ExtractPhrases(string text, int minWords, int maxWords)
        {
            var phrases = new List<string>();
            var tokens = TokenizeText(text);

            for (int i = 0; i <= tokens.Count - minWords; i++)
            {
                for (int j = minWords; j <= maxWords && i + j <= tokens.Count; j++)
                {
                    var phrase = string.Join(" ", tokens.Skip(i).Take(j));
                    if (phrase.Split(' ').Length >= minWords)
                    {
                        phrases.Add(phrase);
                    }
                }
            }

            return phrases;
        }

        private async Task<int> CountMessagesWithKeywordsAsync(string[] keywords)
        {
            var query = _context.Messages.AsQueryable();

            foreach (var keyword in keywords)
            {
                query = query.Where(m => m.Dialog.Contains(keyword));
            }

            return await query.CountAsync();
        }

        private double CalculateClusterCohesion(List<Message> messages, Dictionary<long, List<string>> messageKeywords)
        {
            if (messages.Count < 2)
                return 1.0;

            double totalSimilarity = 0;
            int comparisons = 0;

            for (int i = 0; i < messages.Count; i++)
            {
                for (int j = i + 1; j < messages.Count; j++)
                {
                    var keywords1 = messageKeywords[messages[i].Id];
                    var keywords2 = messageKeywords[messages[j].Id];

                    var intersection = keywords1.Intersect(keywords2).Count();
                    var union = keywords1.Union(keywords2).Count();

                    if (union > 0)
                    {
                        totalSimilarity += (double)intersection / union;
                        comparisons++;
                    }
                }
            }

            return comparisons > 0 ? totalSimilarity / comparisons : 0.0;
        }

        private string GetCategoryDescription(MessageType type)
        {
            return type switch
            {
                MessageType.Command => "Приказы и команды",
                MessageType.Request => "Запросы и просьбы",
                MessageType.Report => "Отчеты и донесения",
                MessageType.Confirmation => "Подтверждения и ответы",
                MessageType.Query => "Вопросы и уточнения",
                MessageType.Coordination => "Координация и согласование",
                MessageType.Technical => "Технические сообщения",
                MessageType.Greeting => "Приветствия и обращения",
                MessageType.Farewell => "Прощания и завершения",
                _ => "Неизвестный тип"
            };
        }

        private class KeywordData
        {
            public string Word { get; set; } = null!;
            public int Frequency { get; set; }
            public int DocumentFrequency { get; set; }
            public HashSet<string> RelatedCallsigns { get; set; } = new();
            public HashSet<string> RelatedAreas { get; set; } = new();
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
        }

        private class KeywordFrequency
        {
            public string Word { get; set; } = null!;
            public int Frequency { get; set; }
            public double Weight { get; set; } = 1.0;
        }
    }

    public class MessageCluster
    {
        public int Id { get; set; }
        public List<string> Keywords { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
        public int Size { get; set; }
        public double AverageSimilarity { get; set; }
        public string Description => $"Кластер {Id}: {string.Join(", ", Keywords.Take(3))}...";
    }
}