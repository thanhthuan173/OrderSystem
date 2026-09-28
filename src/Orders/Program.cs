using DotPulsar;
using DotPulsar.Abstractions;
using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Orders.Messaging;
using Orders.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<OrdersDbContext>(options =>
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

builder.Services.AddHostedService<PaymentSucceededConsumer>();
builder.Services.AddHostedService<PaymentFailedConsumer>();
builder.Services.AddHostedService<ReservationFailedConsumer>();
builder.Services.AddHostedService<ReservationSucceededConsumer>();
builder.Services.AddHostedService<OutboxMessagePublisher>();
builder.Services.AddSingleton<PulsarHealthState>();
builder.Services.AddScoped<OrderService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>("database")
    .AddCheck<PulsarHealthCheck>("pulsar");

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5000",
                "https://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<OrdersDbContext>();

    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("BlazorClient");

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
