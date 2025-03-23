using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Services;
using Moq;
using Microsoft.Extensions.Logging;

namespace Betsson.OnlineWallets.UnitTests
{
    [TestClass]
    public class OnlineWalletBalanceServiceTests
    {
        private readonly Mock<IOnlineWalletRepository> _mockRepo;
        private readonly OnlineWalletService _service;
        private readonly ILogger<OnlineWalletBalanceServiceTests> _logger;

        public OnlineWalletBalanceServiceTests()
        {
            _mockRepo = new Mock<IOnlineWalletRepository>();
            _service = new OnlineWalletService(_mockRepo.Object);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<OnlineWalletBalanceServiceTests>();
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetBalanceAsync_NoEntries_ReturnsZero()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync((OnlineWalletEntry?)null);

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            _logger.LogInformation("Balance returned: {Amount}", result.Amount);
            Assert.AreEqual(0, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetBalanceAsync_ExistingBalance_ReturnsAmount()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 100 });

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            _logger.LogInformation("Balance returned: {Amount}", result.Amount);
            Assert.AreEqual(100, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetBalanceAsync_ExistingBalanceAndValidDeposit_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 100, Amount = 50 });

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            _logger.LogInformation("Balance returned: {Amount}", result.Amount);
            Assert.AreEqual(150, result.Amount);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetBalanceAsync_ExistingBalanceAndValidWithdraw_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 100, Amount = -50 });

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            _logger.LogInformation("Balance returned: {Amount}", result.Amount);
            Assert.AreEqual(50, result.Amount);
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task GetBalanceAsync_DecimalMaximumBalanceAndValidDeposit_ReturnsException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = decimal.MaxValue, Amount = 1 });

            // Act & Assert
            try
            {
                await _service.GetBalanceAsync();
                Assert.Fail("Expected OverflowException was not thrown");
            }
            catch (OverflowException ex)
            {
                _logger.LogInformation("Expected exception caught: {Message}", ex.Message);
                Assert.AreEqual("Value was either too large or too small for a Decimal.", ex.Message, "Exception message mismatch");
            }
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task GetBalanceAsync_DecimalMinimumBalanceAndValidWithdraw_ReturnsException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync(new OnlineWalletEntry { BalanceBefore = decimal.MinValue, Amount = -1 });

            // Act & Assert
            try
            {
                await _service.GetBalanceAsync();
                Assert.Fail("Expected OverflowException was not thrown");
            }
            catch (OverflowException ex)
            {
                _logger.LogInformation("Expected exception caught: {Message}", ex.Message);
                Assert.AreEqual("Value was either too large or too small for a Decimal.", ex.Message, "Exception message mismatch");
            }
        }

    }
}
