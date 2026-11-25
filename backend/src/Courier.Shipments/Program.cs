using Courier.BuildingBlocks;
using Courier.BuildingBlocks.Events;
using Courier.BuildingBlocks.Outbox;
using Courier.Shipments.Clients;
using Courier.Shipments.Data;
using Courier.Shipments.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShipmentsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton<ITrackingNumberGenerator, TrackingNumberGenerator>();
builder.Services.AddScoped<ShipmentFactory>();

builder.Services.AddHttpClient<IPricingClient, HttpPricingClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Pricing"] ?? "http://localhost:5101/"));

builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection("EventBus"));
builder.Services.AddHttpClient<IEventBus, HttpEventBus>();
builder.Services.AddHostedService<OutboxDispatcher<ShipmentsDbContext>>();

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await DatabaseStartup.EnsureCreatedWithRetryAsync<ShipmentsDbContext>(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "shipments" }));
app.MapControllers();
app.Run();
