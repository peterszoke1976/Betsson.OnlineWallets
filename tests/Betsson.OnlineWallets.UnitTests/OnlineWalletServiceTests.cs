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
    public class OnlineWalletServiceTests
    {
        private readonly Mock<IOnlineWalletRepository> _mockRepo;
        private readonly OnlineWalletService _service;
        private readonly ILogger<OnlineWalletServiceTests> _logger;

        public OnlineWalletServiceTests()
        {
            _mockRepo = new Mock<IOnlineWalletRepository>();
            _service = new OnlineWalletService(_mockRepo.Object);
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<OnlineWalletServiceTests>();
        }

        [TestMethod]
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
        public async Task DepositFundsAsync_AddsAmount_ReturnsNewBalance()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 100, Amount = 0 });
            var deposit = new Deposit { Amount = 50 };

            // Act
            var result = await _service.DepositFundsAsync(deposit);

            // Assert
            _logger.LogInformation("New balance returned: {Amount}", result.Amount);
            _mockRepo.Verify(r => r.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(
                e => e.Amount == 50 && e.BalanceBefore == 100)), Times.Once());
        }

        [TestMethod]
        public async Task WithdrawFundsAsync_InsufficientBalance_ThrowsException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 50, Amount = 0 });
            var withdrawal = new Withdrawal { Amount = 100 };

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
            catch (Exception ex)
            {
                _logger.LogError("Unexpected exception thrown: {Message}", ex.Message);
                Assert.Fail($"Expected InsufficientBalanceException, but got {ex.GetType().Name}");
            }


        }



    }
}
