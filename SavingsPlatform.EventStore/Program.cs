using Dapr;
using Marten;
using Microsoft.Extensions.Options;
using Polly;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.EventStore;
using SavingsPlatform.EventStore.Models.Config;
using System.Text.Json;
using System.Text.Json.Serialization;
using Weasel.Core;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddOptions<DocumentStoreConfig>().BindConfiguration(nameof(DocumentStoreConfig));
builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("DocumentStore") 
        ?? throw new NullReferenceException("DocumentStore ConnectionString"));

    options.AutoCreateSchemaObjects = AutoCreate.All;
});
builder.Services.AddScoped<SavingsPlatformEventStore>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? throw new ApplicationException("DAPR_HTTP_PORT is not set as EnvVar");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });

var app = builder.Build();
app.UseCloudEvents();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var rand = new Random(DateTime.UtcNow.Ticks.GetHashCode());

Polly.Retry.AsyncRetryPolicy retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(rand.NextDouble() * 100 + 50));

async Task HandleErrors(Func<Task> func, ILogger<Program> logger)
{
    try
    {
        await retryPolicy.ExecuteAsync(
            async () =>
            {
                await func();
            });
    }
    catch (Exception ex)
    {
        logger.LogError($"Failed to access EventStore: {ex.Message}");
        throw;
    }
}
app.MapPost("/v1/accounts/:handle-created-event",
              [Topic("pubsub", "AccountCreated")] async (AccountCreated @event,
                    SavingsPlatformEventStore store,
                    IOptions<DocumentStoreConfig> dsConfig,
                    ILogger<Program> logger) =>
              {
                  logger.LogInformation($"AccountCreated: {@event}");
                  var platformId = Guid.Parse(dsConfig.Value?.PlatformId ?? throw new NullReferenceException(nameof(DocumentStoreConfig)));
                  logger.LogInformation($"PlatformId: {platformId}");
                  await HandleErrors(async () => await store.AppendEvents(platformId, new[] { @event }), logger);

                  return Results.Accepted();
              });

app.MapPost("v1/accounts/:handle-debited-event",
            [Topic("pubsub", "AccountDebited")] async (AccountDebited @event,
                    SavingsPlatformEventStore store,
                    IOptions<DocumentStoreConfig> dsConfig,
                    ILogger<Program> logger) =>
            {
                logger.LogInformation($"AccountDebited: {@event}");
                var platformId = Guid.Parse(dsConfig.Value?.PlatformId ?? throw new NullReferenceException(nameof(DocumentStoreConfig)));
                logger.LogInformation($"PlatformId: {platformId}");
                await HandleErrors(async () => await store.AppendEvents(platformId, new[] { @event }), logger);

                return Results.Accepted();
            });

app.MapPost("v1/accounts/:handle-credited-event",
            [Topic("pubsub", "AccountCredited")] async (AccountCredited @event,
                   SavingsPlatformEventStore store,
                   IOptions<DocumentStoreConfig> dsConfig,
                   ILogger<Program> logger) =>
            {
                logger.LogInformation($"AccountCredited: {@event}");
                var platformId = Guid.Parse(dsConfig.Value?.PlatformId ?? throw new NullReferenceException(nameof(DocumentStoreConfig)));
                logger.LogInformation($"PlatformId: {platformId}");
                await HandleErrors(async () => await store.AppendEvents(platformId, new[] { @event }), logger);

                return Results.Accepted();
            });


app.MapGet("v1/savings-platform/{platformId:guid}",
    async (Guid platformId, SavingsPlatformEventStore store) =>
    {
        return Results.Ok(await store.GetPlatformOverview(platformId));
    });

app.MapGet("/", () => Results.LocalRedirect("~/swagger"));
app.MapSubscribeHandler();
app.UseRouting();
app.Run();
