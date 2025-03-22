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
        public async Task GetBalanceAsync_ExistingBalanceAndValidDeposit_ReturnsAmount()
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
        public async Task GetBalanceAsync_ExistingBalanceAndValidWithdraw_ReturnsAmount()
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
        public async Task GetBalanceAsync_TooManyRequests_ThrowsException() //Forced error
        {
            // Arrange: Mock the repository to simulate repeated calls
            _mockRepo.SetupSequence(r => r.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 100, Amount = 50 }) // First call
                .ReturnsAsync(new OnlineWalletEntry { BalanceBefore = 150, Amount = -30 }) // Second call
                .Throws(new Exception("Too many requests")); // Simulate rate limiting on third call

            try
            {
                // Act
                await _service.GetBalanceAsync(); // First call should succeed
                await _service.GetBalanceAsync(); // Second call should succeed
                await _service.GetBalanceAsync(); // Third call should throw - This time, instead of returning a value, it throws an exception - This simulates the behavior of a rate limiter blocking further requests
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Exception occurred: {Message}", ex.Message);

                // Assert
                Assert.AreEqual("Too many requests", ex.Message);
            }
        }








    }
}
