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
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
