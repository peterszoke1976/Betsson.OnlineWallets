using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Web;
using Betsson.OnlineWallets.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;

namespace Betsson.OnlineWallets.Data.IntegrationTests
{
    [TestClass]
    public class WalletApiIntegrationTests
    {

        private HttpClient _httpClient;
        private readonly ILogger<WalletApiIntegrationTests> _logger;
        private static readonly string _balanceUri = "http://localhost:5000/onlinewallet/balance";
        private static readonly string _depositUri = "http://localhost:5047/onlinewallet/deposit";
        private static readonly string _withdrawUri = "http://localhost:5047/onlinewallet/withdraw";
        private static readonly WebApplicationFactory<Program> _factory = new();

        public WalletApiIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<WalletApiIntegrationTests>();
        }
        
        [TestInitialize]
        public async Task SetupAsync()
        {
            _httpClient = _factory.CreateClient();
            await ResetBalanceToZeroAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient.Dispose();
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task GetBalance_ReturnBalanceZero()
        {
            //await ResetBalanceToZeroAsync();

            // Act
            var response = await _httpClient.GetAsync(_balanceUri);

            // Assert
            await CheckBalanceAsync(0);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task AddDeposit_ReturnNewBalance()
        {
            //await ResetBalanceToZeroAsync();

            // Add deposit to the wallet, check the response status
            var response = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 1 });
            response.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(1);
        }

        [TestMethod]
        [TestCategory("Positive")]
        public async Task AddDeposit_MakeWithdraw_ReturnZeroBalance()
        {
            //await ResetBalanceToZeroAsync();

            // Add deposit to the wallet, check the response status
            var depositResponse = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 0.1m });
            depositResponse.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(0.1m);

            // Make withdraw from the wallet, check the response status
            var withdrawResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 0.1m });
            withdrawResponse.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(0);
        }







        
        [TestMethod]
        [TestCategory("Negative")]
        public async Task AddDeposit_InvalidNegativeAmount()
        {
            //await ResetBalanceToZeroAsync();

            // Act
            var withdrawResponse = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = -0.1m });

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, withdrawResponse.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await withdrawResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert type and title
            Assert.IsTrue(errorResponse.Errors.ContainsKey("Amount"), "Expected validation error for 'Amount'");
            Assert.AreEqual("'Amount' must be greater than or equal to '0'.", errorResponse.Errors["Amount"][0], "Unexpected error message");
            Assert.AreEqual("One or more validation errors occurred.", errorResponse.Title, "Unexpected error title");
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task MakeWithdrawal_InvalidNegativeAmount()
        {
            //await ResetBalanceToZeroAsync();

            // Act
            var withdrawResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = -0.1m });

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, withdrawResponse.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await withdrawResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert type and title
            Assert.IsTrue(errorResponse.Errors.ContainsKey("Amount"), "Expected validation error for 'Amount'");
            Assert.AreEqual("'Amount' must be greater than or equal to '0'.", errorResponse.Errors["Amount"][0], "Unexpected error message");
            Assert.AreEqual("One or more validation errors occurred.", errorResponse.Title, "Unexpected error title");
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task WithdrawInsufficientAmount_ThrowsInsufficientBalanceException()
        {
            //await ResetBalanceToZeroAsync();

            // Act
            var withdrawResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 0.1m });

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, withdrawResponse.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await withdrawResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert type and title
            Assert.AreEqual("InsufficientBalanceException", errorResponse.Type, "Unexpected exception type");
            Assert.AreEqual("Invalid withdrawal amount. There are insufficient funds.", errorResponse.Title, "Unexpected error title");
        }



        private async Task CheckBalanceAsync(decimal expectedBalance)
        {
            var response = await _httpClient.GetAsync(_balanceUri);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Response status code: {StatusCode}", response.StatusCode);
            var balanceResponse = JsonConvert.DeserializeObject<BalanceResponse>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("Balance returned: {Amount}", balanceResponse.Amount);
            Assert.AreEqual(expectedBalance, balanceResponse.Amount, $"Expected balance {expectedBalance}, but got {balanceResponse.Amount}");
        }
        private async Task ResetBalanceToZeroAsync()
        {
            // Get current balance
            var balanceResponse = await _httpClient.GetFromJsonAsync<Balance>(_balanceUri);
            if (balanceResponse?.Amount > 0)
            {
                // Withdraw the entire balance
                var withdrawResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = balanceResponse.Amount });
                withdrawResponse.EnsureSuccessStatusCode(); // Ensure withdrawal succeeds

                // Verify balance is now 0
                var newBalanceResponse = await _httpClient.GetFromJsonAsync<Balance>(_balanceUri);
                if (newBalanceResponse?.Amount != 0)
                {
                    throw new InvalidOperationException($"Failed to reset balance to 0. Current balance: {newBalanceResponse.Amount}");
                }
            }
            _logger.LogInformation("Balance reset to 0");
        }

    }

}
