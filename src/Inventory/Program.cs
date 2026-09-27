using DotPulsar;
using DotPulsar.Abstractions;
using Inventory.Data;
using Inventory.Messaging;
using Inventory.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IPulsarClient>(_ =>
{
    var serviceUrl = builder.Configuration["PULSAR_SERVICE_URL"]
        ?? throw new InvalidOperationException("PULSAR_SERVICE_URL is not configured");

    return PulsarClient
        .Builder()
        .ServiceUrl(new Uri(serviceUrl))
        .Build();
});

builder.Services.AddHostedService<OrderPlacedConsumer>();
builder.Services.AddHostedService<OutboxMessagePublisher>();
builder.Services.AddSingleton<PulsarHealthState>();
builder.Services.AddScoped<InventoryService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>("database")
    .AddCheck<PulsarHealthCheck>("pulsar");

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy
            .WithOrigins("https://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("BlazorClient");

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
