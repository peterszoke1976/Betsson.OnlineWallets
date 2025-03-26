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
    public class OnlineWalletWithdrawServiceTests
    {
        private readonly Mock<IOnlineWalletRepository> _mockRepo;
        private readonly OnlineWalletService _service;
        private readonly ILogger<OnlineWalletWithdrawServiceTests> _logger;

        public OnlineWalletWithdrawServiceTests()
        {
            _mockRepo = new Mock<IOnlineWalletRepository>();
            _service = new OnlineWalletService(_mockRepo.Object);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<OnlineWalletWithdrawServiceTests>();
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task WithdrawFundsAsync_WithdrawAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 50 });
            var withdrawal = new Withdrawal { Amount = 10 };

            // Act
            var result = await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(40, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task WithdrawFundsAsync_WithdrawFullBalance_ReturnsZeroBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 15015 });
            var withdrawal = new Withdrawal { Amount = 15015 };

            // Act
            var result = await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(0, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task WithdrawFundsAsync_WithdrawDecimalAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 1.15m });
            var withdrawal = new Withdrawal { Amount = 0.45m };

            // Act
            var result = await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(0.7m, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task WithdrawFundsAsync_WithdrawDecimalMaxValue_ReturnsZeroBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = decimal.MaxValue });
            var withdrawal = new Withdrawal { Amount = decimal.MaxValue };

            // Act
            var result = await _service.WithdrawFundsAsync(withdrawal);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            Assert.AreEqual(0, result.Amount);
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task WithdrawFundsAsync_WithdrawInsufficientAmount_ThrowsInsufficientBalanceException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 50 });
            var withdrawal = new Withdrawal { Amount = 50.1m };

            // Act & Assert
            try
            {
                await _service.WithdrawFundsAsync(withdrawal);
                Assert.Fail("Expected InsufficientBalanceException was not thrown");
            }
            catch (InsufficientBalanceException ex)
            {
                _logger.LogInformation("Exception thrown as expected: {Message}", ex.Message);
                Assert.AreEqual("Invalid withdrawal amount. There are insufficient funds.", ex.Message, "Exception message mismatch");
            }
        }

    }
}
