using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.PaymentProxy.Services;

namespace SavingsPlatform.PaymentProxy.ApiClients
{
    public class SavingsPlatformApiClient : ISavingsPlatformApiClient
    {
        private readonly DaprClient _daprClient;
        private readonly ProxyConfig _proxyCfg;
        private readonly ILogger<SavingsPlatformApiClient> _logger;

        private const string ProcessFileEndpoint = "v1/savings-accounts/:process-file";


        public SavingsPlatformApiClient(
            DaprClient daprClient,
            IOptions<ProxyConfig> proxyCfg,
            ILogger<SavingsPlatformApiClient> logger)
        {
            _daprClient = daprClient;
            _proxyCfg = proxyCfg.Value;
            _logger = logger;

        }

        public async Task ProcessFile(DepositRequestsFile request)
        {
            var dprReq = _daprClient.CreateInvokeMethodRequest<DepositRequestsFile>(
                               _proxyCfg.SavingsPlatformAppName,
                               ProcessFileEndpoint,
                               request);

            var res = await _daprClient.InvokeMethodWithResponseAsync(dprReq);

            if (!res.IsSuccessStatusCode)
            {
                var resContent = await res.Content?.ReadAsStringAsync() ?? string.Empty;
                var logMsg = $"Failed to process DepositRequestsFile. Status code: {res.StatusCode}, Details: {resContent}";

                _logger.LogError(logMsg);
                throw new InvalidOperationException(logMsg);
            }
            else
            {
                _logger.LogInformation("DepositRequestsFile processed successfully.");
            }
        }

    }
}
