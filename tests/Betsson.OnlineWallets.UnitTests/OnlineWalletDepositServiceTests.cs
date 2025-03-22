using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Moq;
using Microsoft.Extensions.Logging;

namespace Betsson.OnlineWallets.UnitTests
{
    [TestClass]
    public class OnlineWalletDepositServiceTests
    {
        private readonly Mock<IOnlineWalletRepository> _mockRepo;
        private readonly OnlineWalletService _service;
        private readonly ILogger<OnlineWalletDepositServiceTests> _logger;

        public OnlineWalletDepositServiceTests()
        {
            _mockRepo = new Mock<IOnlineWalletRepository>();
            _service = new OnlineWalletService(_mockRepo.Object);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<OnlineWalletDepositServiceTests>();
        }
                
        [TestMethod]
        [TestCategory("Positive")]
        public async Task DepositFundsAsync_AddsAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 0 });
            var deposit = new Deposit { Amount = 50 };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            _mockRepo.Verify(r => r.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(
                e => e.Amount == 50 && e.BalanceBefore == 0)), Times.Once());
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task DepositFundsAsync_ExistingBalanceAndAddsAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 10 });
            var deposit = new Deposit { Amount = 50 };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            _mockRepo.Verify(r => r.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(
                e => e.Amount == 50 && e.BalanceBefore == 10)), Times.Once());
        }








    }
}
