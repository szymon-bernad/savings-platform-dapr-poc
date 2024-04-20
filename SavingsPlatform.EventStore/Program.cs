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
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
                      policy =>
                      {
                          policy.WithOrigins("*")
                                .WithMethods("GET", "OPTIONS");
                      });
});

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

async Task RunAndHandleErrors(Func<Task> func, ILogger<Program> logger)
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

Guid GetPlatformId(string? platformId, IOptions<DocumentStoreConfig> dsConfig)
{
    platformId = string.IsNullOrWhiteSpace(platformId) ? 
        dsConfig?.Value?.PlatformId 
        : platformId;

    if (platformId is null)
        throw new NullReferenceException(nameof(DocumentStoreConfig));

    return Guid.Parse(platformId);
}

app.MapPost("/v1/accounts/:handle-created-event",
              [Topic("pubsub", "accountcreated")] async (AccountCreated @event,
                    SavingsPlatformEventStore store,
                    IOptions<DocumentStoreConfig> dsConfig,
                    ILogger<Program> logger) =>
              {
                  logger.LogInformation($"AccountCreated: {@event}");
                  var platformId = GetPlatformId(@event.PlatformId, dsConfig).ToString();

                  await RunAndHandleErrors(async () => await store.AppendEvents(new[] { @event with { PlatformId = platformId } } ), logger);

                  return Results.Accepted();
              });

app.MapPost("v1/accounts/:handle-debited-event",
            [Topic("pubsub", "accountdebited")] async (AccountDebited @event,
                    SavingsPlatformEventStore store,
                    IOptions<DocumentStoreConfig> dsConfig,
                    ILogger<Program> logger) =>
            {
                logger.LogInformation($"AccountDebited: {@event}");
                var platformId = GetPlatformId(@event.PlatformId, dsConfig).ToString();
                await RunAndHandleErrors(async () => await store.AppendEvents(new[] { @event with { PlatformId = platformId } }), logger);

                return Results.Accepted();
            });

app.MapPost("v1/accounts/:handle-credited-event",
            [Topic("pubsub", "accountcredited")] async (AccountCredited @event,
                   SavingsPlatformEventStore store,
                   IOptions<DocumentStoreConfig> dsConfig,
                   ILogger<Program> logger) =>
            {
                logger.LogInformation($"AccountCredited: {@event}");
                var platformId = GetPlatformId(@event.PlatformId, dsConfig).ToString();
                await RunAndHandleErrors(async () => await store.AppendEvents(new[] { @event with { PlatformId = platformId } }), logger);

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
app.UseCors("AllowAll");
app.Run();

