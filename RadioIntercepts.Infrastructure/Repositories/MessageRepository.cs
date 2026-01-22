using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Interfaces;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _db;

        public MessageRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Message>> GetAllAsync()
        {
            return await _db.Messages
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .OrderByDescending(m => m.DateTime)
                .ToListAsync();
        }

        public async Task AddAsync(Message message)
        {
            if (message == null)
                return;

            // Очищаем трекер перед работой
            _db.ChangeTracker.Clear();

            // 1. Area: ЗАГРУЖАЕМ ВСЕ из БД и ищем в памяти
            Area areaEntity = null;
            if (message.Area != null && !string.IsNullOrWhiteSpace(message.Area.Key))
            {
                // Загружаем все Areas один раз
                var allAreas = await _db.Areas
                    .AsNoTracking()
                    .ToListAsync();

                // Ищем точное совпадение в памяти
                areaEntity = allAreas.FirstOrDefault(a =>
                    string.Equals(a.Key, message.Area.Key, System.StringComparison.Ordinal));
            }

            // Если не нашли - СОЗДАТЬ
            if (areaEntity == null && message.Area != null && !string.IsNullOrWhiteSpace(message.Area.Key))
            {
                areaEntity = new Area
                {
                    Name = message.Area.Name ?? message.Area.Key,
                    Key = message.Area.Key
                };
                _db.Areas.Add(areaEntity);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
            }
            else if (areaEntity == null)
            {
                // Дефолтный Area
                var allAreas = await _db.Areas
                    .AsNoTracking()
                    .ToListAsync();

                areaEntity = allAreas.FirstOrDefault(a => a.Key == "unknown");
                if (areaEntity == null)
                {
                    areaEntity = new Area { Name = "❓", Key = "unknown" };
                    _db.Areas.Add(areaEntity);
                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                }
            }

            // 2. Frequency: НАЙТИ или СОЗДАТЬ
            var freqValue = message.Frequency?.Value ?? "0";
            var allFrequencies = await _db.Frequencies
                .AsNoTracking()
                .ToListAsync();

            var frequencyEntity = allFrequencies.FirstOrDefault(f => f.Value == freqValue);

            if (frequencyEntity == null)
            {
                frequencyEntity = new Frequency { Value = freqValue };
                _db.Frequencies.Add(frequencyEntity);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
            }

            // 3. Создаем Message с внешними ключами
            var messageEntity = new Message
            {
                DateTime = message.DateTime,
                AreaId = areaEntity.Id,
                FrequencyId = frequencyEntity.Id,
                Unit = message.Unit ?? "-",
                Dialog = message.Dialog ?? "-"
            };

            _db.Messages.Add(messageEntity);
            await _db.SaveChangesAsync();

            // Очищаем трекер перед работой с MessageCallsign
            _db.ChangeTracker.Clear();

            // 4. Callsigns: обрабатываем ПОСЛЕ сохранения Message
            if (message.MessageCallsigns != null && message.MessageCallsigns.Any())
            {
                // Получаем уникальные имена позывных
                var callsignNames = message.MessageCallsigns
                    .Where(mc => mc.Callsign != null && !string.IsNullOrWhiteSpace(mc.Callsign.Name))
                    .Select(mc => mc.Callsign.Name)
                    .Distinct()
                    .ToList();

                foreach (var callsignName in callsignNames)
                {
                    // Загружаем все Callsigns
                    var allCallsigns = await _db.Callsigns
                        .AsNoTracking()
                        .ToListAsync();

                    // НАЙТИ или СОЗДАТЬ Callsign
                    var callsignEntity = allCallsigns.FirstOrDefault(c => c.Name == callsignName);

                    if (callsignEntity == null)
                    {
                        callsignEntity = new Callsign { Name = callsignName };
                        _db.Callsigns.Add(callsignEntity);
                        await _db.SaveChangesAsync();
                        _db.ChangeTracker.Clear();
                    }

                    // Проверяем, нет ли уже такой связи
                    var allMessageCallsigns = await _db.MessageCallsigns
                        .AsNoTracking()
                        .ToListAsync();

                    bool linkExists = allMessageCallsigns
                        .Any(mc => mc.MessageId == messageEntity.Id && mc.CallsignId == callsignEntity.Id);

                    if (!linkExists)
                    {
                        // СОЗДАТЬ связь MessageCallsign
                        var messageCallsign = new MessageCallsign
                        {
                            MessageId = messageEntity.Id,
                            CallsignId = callsignEntity.Id
                        };

                        _db.MessageCallsigns.Add(messageCallsign);
                        await _db.SaveChangesAsync();
                        _db.ChangeTracker.Clear();
                    }
                }
            }
        }

        public async Task AddRangeAsync(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                await AddAsync(message);
            }
        }

        public async Task UpdateAsync(Message message)
        {
            _db.ChangeTracker.Clear();
            _db.Messages.Update(message);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Message message)
        {
            _db.ChangeTracker.Clear();

            // Удаляем сначала связи MessageCallsign
            var allMessageCallsigns = await _db.MessageCallsigns
                .AsNoTracking()
                .ToListAsync();

            var links = allMessageCallsigns
                .Where(mc => mc.MessageId == message.Id)
                .ToList();

            if (links.Any())
            {
                _db.MessageCallsigns.RemoveRange(links);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
            }

            // Удаляем само сообщение
            _db.Messages.Remove(message);
            await _db.SaveChangesAsync();
        }
    }
}