using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.Infrastructure.Repositories;
using RadioIntercepts.Core.Models;
using System.Threading.Tasks;
using Xunit;

namespace RadioIntercepts.Tests
{
    public class MessageRepositoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AddMessageAsync_ShouldAddMessage()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new MessageRepository(context);

            var message = new Message
            {
               // Timestamp = DateTime.Now,
                //Frequency = 145.4250,
                Dialog = "Test message"
            };

            // Act
            await repository.AddAsync(message);
            var count = await context.Messages.CountAsync();

            // Assert
            Assert.Equal(1, count);
        }
    }
}
