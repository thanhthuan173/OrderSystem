using DotPulsar;
using DotPulsar.Abstractions;
using Inventory.Data;
using Inventory.Messaging;
using Inventory.Services;
using Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<PulsarOptions>(
    builder.Configuration.GetSection("Pulsar"));

builder.Services.AddSingleton<IPulsarClient>(sp =>
{
    var options = sp
        .GetRequiredService<IOptions<PulsarOptions>>()
        .Value;

    if (string.IsNullOrWhiteSpace(options.ServiceUrl))
    {
        throw new InvalidOperationException(
            "Pulsar service URL is not configured.");
    }

    return PulsarClient
        .Builder()
        .ServiceUrl(new Uri(options.ServiceUrl))
        .Build();
});

builder.Services.AddHostedService<InventoryOrderPlacedConsumer>();
builder.Services.AddSingleton<IPulsarEventPublisher, PulsarEventPublisher>();
builder.Services.AddScoped<InventoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
