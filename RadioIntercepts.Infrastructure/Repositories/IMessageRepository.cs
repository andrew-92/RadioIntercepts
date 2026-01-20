using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Infrastructure.Repositories
{
    public interface IMessageRepository
    {
        Task<List<Message>> GetAllAsync();
        Task AddAsync(Message message);
        Task UpdateAsync(Message message);
        Task DeleteAsync(Message message);
    }
}