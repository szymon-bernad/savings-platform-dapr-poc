using Carter;
using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Contracts.Platform;
using SavingsPlatform.Dashboard.Api.Config;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;
using DaprClient = Dapr.Client.DaprClient;

namespace SavingsPlatform.Dashboard.Api.Api.Modules
{
    public class DashboardModule : ICarterModule
    {
        private static ServicesConfig DefaultServicesConfig = new ServicesConfig
        {
            SavingsPlatformApi = "dapr-savings-acc",
            EventStoreApi = "dapr-savings-evt"
        };

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/v1/dashboard/platforms/:get-ids",
                async (IOptions<ServicesConfig> servicesCfg, DaprClient daprClient) =>
                {
                    var cfg = DefaultServicesConfig;

                    var platformIds = await daprClient.InvokeMethodAsync<ICollection<string>>(
                        HttpMethod.Get,
                        cfg.SavingsPlatformApi,
                        "v1/platforms/:get-ids");
                    return Results.Ok(platformIds);
                });

            app.MapGet("/v1/dashboard/platforms/{platformId}",
                async (string platformId,
                        IOptions<ServicesConfig> servicesCfg,
                       DaprClient daprClient) =>
                {
                    var cfg = DefaultServicesConfig;

                    var platform = await daprClient.InvokeMethodAsync<SavingsPlatformSummary>(
                        HttpMethod.Get,
                        cfg.EventStoreApi,
                        $"v1/savings-platform/{platformId}");
                    return Results.Ok(platform);
                });
        }
    }
}
