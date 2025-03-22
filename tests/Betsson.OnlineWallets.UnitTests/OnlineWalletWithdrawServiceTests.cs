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
        [TestCategory("Negative")]
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
