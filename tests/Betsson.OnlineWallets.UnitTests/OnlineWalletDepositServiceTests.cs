using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
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
        public async Task DepositFundsAsync_DepositAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 0 });
            var deposit = new Deposit { Amount = 50 };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(50, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task DepositFundsAsync_DepositLowDecimalAmount_ReturnsNewBalance()
        {
            // Arrange
            
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 0 });
            var deposit = new Deposit { Amount = 0.1m };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(0.1m, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task DepositFundsAsync_DepositMaxDecimalValue_ReturnsNewBalance()
        {
            // Arrange

            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 0 });
            var deposit = new Deposit { Amount = decimal.MaxValue };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(decimal.MaxValue, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task DepositFundsAsync_ExistingBalanceAndDepositAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 100.28m });
            var deposit = new Deposit { Amount = 50 };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(150.28m, result.Amount);
        }

    }
}
