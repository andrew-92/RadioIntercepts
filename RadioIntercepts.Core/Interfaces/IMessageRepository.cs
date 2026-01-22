using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Core.Interfaces
{
    public interface IMessageRepository
    {
        Task<List<Message>> GetAllAsync();
        Task AddAsync(Message message);
        Task UpdateAsync(Message message);
        Task DeleteAsync(Message message);
    }
}