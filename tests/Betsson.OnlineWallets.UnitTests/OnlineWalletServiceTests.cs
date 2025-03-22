using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Moq;

namespace Betsson.OnlineWallets.UnitTests
{
    [TestClass]
    public class OnlineWalletServiceTests
    {
        private readonly Mock<IOnlineWalletRepository> _mockRepo;
        private readonly OnlineWalletService _service;

        public OnlineWalletServiceTests()
        {
            _mockRepo = new Mock<IOnlineWalletRepository>();
            _service = new OnlineWalletService(_mockRepo.Object);
        }

        [TestMethod]
        public async Task GetBalanceAsync_NoEntries_ReturnsZero()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetLastOnlineWalletEntryAsync()).ReturnsAsync((OnlineWalletEntry?)null);

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            //result.Amount.Should().Be(0);
            Assert.AreEqual(0, result.Amount); // MSTest alternative
        }

        

        
    }
}
