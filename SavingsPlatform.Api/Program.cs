using Carter;
using Microsoft.AspNetCore.Http.Json;
using SavingsPlatform.Accounts.DependencyInjection;
using SavingsPlatform.Accounts.Handlers;
using SavingsPlatform.Contracts.Accounts.Commands;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? throw new ApplicationException("DAPR_HTTP_PORT is not set as EnvVar");
builder.Services.AddDaprClient(dpr => { dpr.UseJsonSerializationOptions(jsonOptions); });
builder.Services.AddSavingsAccounts(builder.Configuration, Int32.Parse(daprHttpPort));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddCarter();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(new Assembly[]
    {
        Assembly.GetExecutingAssembly(),
        Assembly.GetAssembly(typeof(PublishEventsCommand))!,
        Assembly.GetAssembly(typeof(PublishEventsCommandHandler))!
    });
});

var app = builder.Build();
app.UseCloudEvents();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.LocalRedirect("~/swagger"));
app.MapCarter();

app.MapSubscribeHandler();
app.UseRouting();
app.MapActorsHandlers();

app.Run();
