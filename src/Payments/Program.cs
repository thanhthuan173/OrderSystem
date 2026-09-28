using DotPulsar;
using DotPulsar.Abstractions;
using Microsoft.EntityFrameworkCore;
using Payments.Data;
using Payments.Messaging;
using Payments.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<PaymentsDbContext>(options =>
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

builder.Services.AddSingleton<PulsarHealthState>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddHostedService<OutboxMessagePublisher>();
builder.Services.AddHostedService<ReservationSucceededConsumer>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentsDbContext>("database")
    .AddCheck<PulsarHealthCheck>("pulsar");

var app = builder.Build();

// Configure the HTTP request pipeline.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<PaymentsDbContext>();

    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
