using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Web;
using Betsson.OnlineWallets.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;
using System.Text;

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
        [DoNotParallelize]
        public async Task GetBalance_ReturnBalanceZero()
        {
            // Act
            var response = await _httpClient.GetAsync(_balanceUri);

            // Assert
            await CheckBalanceAsync(0);
        }

        [TestMethod]
        [TestCategory("Positive")]
        [DoNotParallelize]
        public async Task AddDeposit_ReturnNewBalance()
        {
            // Add deposit to the wallet, check the response status
            var response = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 1 });
            response.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(1);
        }

        [TestMethod]
        [TestCategory("Positive")]
        [DoNotParallelize]
        public async Task AddDeposit_MakeWithdrawal_MaxDecimalValue_ReturnZeroBalance()
        {
            // Add deposit to the wallet, check the response status
            var depositResponse = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = decimal.MaxValue });
            depositResponse.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(decimal.MaxValue);

            // Make a withdraw from the wallet, check the response status
            var withdrawalResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = decimal.MaxValue });
            withdrawalResponse.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(0);
        }

        [TestMethod]
        [TestCategory("Positive")]
        [DoNotParallelize]
        public async Task AddMultipleDeposits_ReturnNewBalance()
        {
            // Add multiple deposits to the wallet, check the response status
            var deposti1 = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 1 });
            deposti1.EnsureSuccessStatusCode();

            var deposti2 = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 100.8m });
            deposti2.EnsureSuccessStatusCode();

            var deposti3 = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 32.7m });
            deposti3.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(134.5m);
        }

        [TestMethod]
        [TestCategory("Positive")]
        [DoNotParallelize]
        public async Task AddDeposit_MakeWithdraw_ReturnZeroBalance()
        {
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
        [TestCategory("Positive")]
        [DoNotParallelize]
        public async Task AddDeposit_MakeMultipleWithdrawals_ReturnNewBalance()
        {
            // Add deposit to the wallet, check the response status
            var depositResponse = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 1000 });
            depositResponse.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(1000);

            // Make multiple withdrawals from the wallet, check the response status
            var withdraw1 = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 0.1m });
            withdraw1.EnsureSuccessStatusCode();

            var withdraw2 = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 734.8m });
            withdraw2.EnsureSuccessStatusCode();

            var withdraw3 = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 15 });
            withdraw3.EnsureSuccessStatusCode();

            // Get and check the new balance
            await CheckBalanceAsync(250.1m);
        }

        [TestMethod]
        [TestCategory("Positive")]
        [DoNotParallelize]
        public async Task AddMultipleDeposits_MakeMultipleWithdrawals_ReturnNewBalance()
        {
            // Add deposits and withdrawals, check the balance
            // First
            var deposit1 = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 1000 });
            deposit1.EnsureSuccessStatusCode();

            var withdraw1 = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 0.1m });
            withdraw1.EnsureSuccessStatusCode();

            await CheckBalanceAsync(999.9m);

            // Second
            var deposit2 = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 14.8m });
            deposit2.EnsureSuccessStatusCode();

            var withdraw2 = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 555.5m });
            withdraw2.EnsureSuccessStatusCode();

            await CheckBalanceAsync(459.2m);

            // Third
            var deposit3 = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 15326 });
            deposit3.EnsureSuccessStatusCode();

            var withdraw3 = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 15784.2m });
            withdraw3.EnsureSuccessStatusCode();

            await CheckBalanceAsync(1);
        }









        [TestMethod]
        [TestCategory("Negative")]
        public async Task AddDeposit_InvalidNegativeAmount()
        {
            // Act
            var depositResponse = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = -0.1m });

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, depositResponse.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await depositResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert error message
            Assert.IsTrue(errorResponse.Errors.ContainsKey("Amount"), "Expected validation error for 'Amount'");
            Assert.AreEqual("'Amount' must be greater than or equal to '0'.", errorResponse.Errors["Amount"][0], "Unexpected error message");
            Assert.AreEqual("One or more validation errors occurred.", errorResponse.Title, "Unexpected error title");
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task MakeWithdraw_InvalidNegativeAmount()
        {
            // Act
            var withdrawResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = -0.1m });

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, withdrawResponse.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await withdrawResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert error message
            Assert.IsTrue(errorResponse.Errors.ContainsKey("Amount"), "Expected validation error for 'Amount'");
            Assert.AreEqual("'Amount' must be greater than or equal to '0'.", errorResponse.Errors["Amount"][0], "Unexpected error message");
            Assert.AreEqual("One or more validation errors occurred.", errorResponse.Title, "Unexpected error title");
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task WithdrawInsufficientAmount_ThrowsInsufficientBalanceException()
        {
            // Arrange
            var depositResponse = await _httpClient.PostAsJsonAsync(_depositUri, new Deposit { Amount = 1 });

            // Act
            var withdrawResponse = await _httpClient.PostAsJsonAsync(_withdrawUri, new Withdrawal { Amount = 1.1m });

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, withdrawResponse.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await withdrawResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert error message
            Assert.AreEqual("InsufficientBalanceException", errorResponse.Type, "Unexpected exception type");
            Assert.AreEqual("Invalid withdrawal amount. There are insufficient funds.", errorResponse.Title, "Unexpected error title");

            // Get and check the balance remain unchanged
            await CheckBalanceAsync(1);
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task AddDeposit_InvalidType_ReturnsValidationError()
        {
            // Act: Send raw JSON with a string value for Amount
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(new { Amount = "abc" }),
                Encoding.UTF8,
                "application/json"
            );
            var response = await _httpClient.PostAsync(_depositUri, jsonContent);

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert error message
            Assert.AreEqual("One or more validation errors occurred.", errorResponse.Title, "Unexpected error title");
            Assert.AreEqual("The JSON value could not be converted to System.Decimal. Path: $.Amount | LineNumber: 0 | BytePositionInLine: 15.", errorResponse.Errors["$.Amount"][0], "Unexpected error message");
            Assert.AreEqual("The depositRequest field is required.", errorResponse.Errors["depositRequest"][0], "Unexpected error message");
        }

        [TestMethod]
        [TestCategory("Negative")]
        public async Task MakeWithdrawal_InvalidType_ReturnsValidationError()
        {
            // Act: Send raw JSON with a string value for Amount
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(new { Amount = "abc" }),
                Encoding.UTF8,
                "application/json"
            );
            var response = await _httpClient.PostAsync(_withdrawUri, jsonContent);

            // Assert status code
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Expected 400 Bad Request status code");

            // Read and deserialize response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response content: {Content}", responseContent);
            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);

            // Assert error message
            Assert.AreEqual("One or more validation errors occurred.", errorResponse.Title, "Unexpected error title");
            Assert.AreEqual("The JSON value could not be converted to System.Decimal. Path: $.Amount | LineNumber: 0 | BytePositionInLine: 15.", errorResponse.Errors["$.Amount"][0], "Unexpected error message");
            Assert.AreEqual("The withdrawalRequest field is required.", errorResponse.Errors["withdrawalRequest"][0], "Unexpected error message");
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
