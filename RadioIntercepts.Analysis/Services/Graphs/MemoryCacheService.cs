// RadioIntercepts.Infrastructure/Caching/MemoryCacheService.cs
using RadioIntercepts.Analysis.Interfaces.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace RadioIntercepts.Analysis.Services.Graphs
{
    public class MemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly Timer _cleanupTimer;
        private readonly object _lock = new object();

        public MemoryCacheService()
        {
            // Очистка устаревших записей каждые 5 минут
            _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (!entry.IsExpired)
                {
                    value = (T)entry.Value;
                    entry.LastAccess = DateTime.UtcNow;
                    return true;
                }

                // Удаляем просроченную запись
                _cache.TryRemove(key, out _);
            }

            value = default;
            return false;
        }

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            var entry = new CacheEntry
            {
                Key = key,
                Value = value,
                Expiration = expiration,
                CreatedAt = DateTime.UtcNow,
                LastAccess = DateTime.UtcNow
            };

            _cache[key] = entry;
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        private void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        private class CacheEntry
        {
            public string Key { get; set; }
            public object Value { get; set; }
            public TimeSpan Expiration { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastAccess { get; set; }

            public bool IsExpired => DateTime.UtcNow > CreatedAt.Add(Expiration);
        }

        // Статистика кэша
        public CacheStatistics GetStatistics()
        {
            var stats = new CacheStatistics
            {
                TotalEntries = _cache.Count,
                MemoryUsage = GetApproximateMemoryUsage()
            };

            foreach (var entry in _cache.Values)
            {
                stats.TotalHits += entry.LastAccess != default ? 1 : 0;
            }

            return stats;
        }

        private long GetApproximateMemoryUsage()
        {
            long total = 0;

            foreach (var entry in _cache.Values)
            {
                total += GetObjectSize(entry.Value);
            }

            return total;
        }

        private long GetObjectSize(object obj)
        {
            if (obj == null) return 0;

            // Приблизительный расчет размера
            var type = obj.GetType();

            if (type.IsValueType)
            {
                return System.Runtime.InteropServices.Marshal.SizeOf(type);
            }

            if (obj is string str)
            {
                return System.Text.Encoding.UTF8.GetByteCount(str);
            }

            // Для сложных объектов возвращаем примерный размер
            return 100; // байт
        }
    }

    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int TotalHits { get; set; }
        public long MemoryUsage { get; set; } // в байтах

        public double HitRate => TotalEntries > 0 ? (double)TotalHits / TotalEntries : 0;
    }

    // Расширенный кэш с политиками
    public class AdvancedMemoryCacheService : MemoryCacheService
    {
        private readonly int _maxSize;
        private readonly LinkedList<string> _accessOrder = new();
        private readonly Dictionary<string, LinkedListNode<string>> _accessMap = new();

        public AdvancedMemoryCacheService(int maxSize = 10000)
        {
            _maxSize = maxSize;
        }

        public new bool TryGet<T>(string key, out T value)
        {
            var result = base.TryGet(key, out value);

            if (result && _accessMap.ContainsKey(key))
            {
                // Обновляем порядок доступа (перемещаем в начало)
                var node = _accessMap[key];
                _accessOrder.Remove(node);
                _accessOrder.AddFirst(node);
            }

            return result;
        }

        public new void Set<T>(string key, T value, TimeSpan expiration)
        {
            // Если достигли максимального размера, удаляем наименее используемый элемент
            if (_accessOrder.Count >= _maxSize)
            {
                var lastNode = _accessOrder.Last;
                if (lastNode != null)
                {
                    Remove(lastNode.Value);
                }
            }

            base.Set(key, value, expiration);

            // Добавляем в порядок доступа
            var node = new LinkedListNode<string>(key);
            _accessOrder.AddFirst(node);
            _accessMap[key] = node;
        }

        public new void Remove(string key)
        {
            base.Remove(key);

            if (_accessMap.TryGetValue(key, out var node))
            {
                _accessOrder.Remove(node);
                _accessMap.Remove(key);
            }
        }

        public new void Clear()
        {
            base.Clear();
            _accessOrder.Clear();
            _accessMap.Clear();
        }
    }
}