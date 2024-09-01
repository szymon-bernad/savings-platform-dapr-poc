using System.Text.Json.Serialization;
using System.Text.Json;
using Carter;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.PaymentProxy.Services;
using Microsoft.AspNetCore.Http.Json;
using SavingsPlatform.PaymentProxy;
using SavingsPlatform.PaymentProxy.ApiClients;
using OpenTelemetry.Resources;
using System.Reflection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? throw new ApplicationException("DAPR_HTTP_PORT is not set as EnvVar");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });
builder.Services.AddOptions<SavingsAccountsStateStoreConfig>().Bind(builder.Configuration.GetSection("StateStore"));
builder.Services.AddOptions<ProxyConfig>().Bind(builder.Configuration.GetSection("ProxyCfg"));
builder.Services.AddScoped<IAccountExternalRefService, AccountExternalRefService>();
builder.Services.AddScoped<ISavingsPlatformApiClient, SavingsPlatformApiClient>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();

builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(serviceName: Assembly.GetExecutingAssembly().GetName().Name))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter()
          .AddZipkinExporter(opts =>
          {
              opts.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
          }))
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter());

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();
app.UseCloudEvents();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.LocalRedirect("~/swagger"));
app.MapCarter();
app.UseRouting();
app.MapSubscribeHandler();

app.Run();
