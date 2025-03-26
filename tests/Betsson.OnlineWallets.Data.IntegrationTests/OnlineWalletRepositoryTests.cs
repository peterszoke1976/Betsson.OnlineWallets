using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Betsson.OnlineWallets.Data.IntegrationTests
{
    [TestClass]
    public class OnlineWalletRepositoryTests
    {
        private OnlineWalletContext _dbContext;
        private OnlineWalletRepository _repository;
        private ILogger<OnlineWalletRepositoryTests> _logger;

        public OnlineWalletRepositoryTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<OnlineWalletRepositoryTests>();
        }

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OnlineWalletContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            _dbContext = new OnlineWalletContext(options);
            _repository = new OnlineWalletRepository(_dbContext);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetLastOnlineWalletEntryAsync_NoEntries_ReturnsNull()
        {
            // Act
            var result = await _repository.GetLastOnlineWalletEntryAsync();
            _logger.LogInformation("Result: {Result}", result);

            // Assert
            Assert.IsNull(result, "Expected null when no transactions exist.");
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task InsertOnlineWalletEntryAsync_AddsEntry_CanRetrieveIt()
        {
            // Arrange
            var newEntry = new OnlineWalletEntry
            {
                Amount = 100,
                BalanceBefore = 200,
                EventTime = DateTimeOffset.UtcNow
            };

            // Act
            await _repository.InsertOnlineWalletEntryAsync(newEntry);
            var retrievedEntry = await _repository.GetLastOnlineWalletEntryAsync();
            _logger.LogInformation("Result: {retrievedEntry}", JsonSerializer.Serialize(retrievedEntry));

            // Assert
            Assert.AreEqual(newEntry.Amount, retrievedEntry.Amount, "Amounts should match.");
            Assert.AreEqual(newEntry.BalanceBefore, retrievedEntry.BalanceBefore, "Balances should match.");
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetLastOnlineWalletEntryAsync_MultipleEntries_ReturnsLatest()
        {
            // Arrange
            var earlierEntry = new OnlineWalletEntry
            {
                Amount = 50,
                BalanceBefore = 150,
                EventTime = DateTimeOffset.UtcNow.AddMinutes(-10)
            };

            var latestEntry = new OnlineWalletEntry
            {
                Amount = 200,
                BalanceBefore = 350,
                EventTime = DateTimeOffset.UtcNow
            };

            await _repository.InsertOnlineWalletEntryAsync(latestEntry);
            await _repository.InsertOnlineWalletEntryAsync(earlierEntry);

            var count = await _dbContext.Transactions.CountAsync();
            _logger.LogInformation("Transaction count: {Count}", count);
            Assert.AreEqual(2, count);

            // Act
            var result = await _repository.GetLastOnlineWalletEntryAsync();
            _logger.LogInformation("Result: {Result}", JsonSerializer.Serialize(result));

            // Assert
            Assert.IsNotNull(result, "Expected a non-null result.");
            Assert.AreEqual(latestEntry.Amount, result.Amount, "Should return the latest transaction amount.");
            Assert.AreEqual(latestEntry.BalanceBefore, result.BalanceBefore, "Should return the balance before the latest transaction.");
            Assert.AreEqual(latestEntry.EventTime, result.EventTime, "Should return the latest transaction event time.");
        }
    }
}
