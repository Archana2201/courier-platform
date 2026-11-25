using Courier.BuildingBlocks;
using Courier.Pricing.Data;
using Courier.Pricing.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PricingDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton(QuoteCalculator.CreateDefault());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await DatabaseStartup.EnsureCreatedWithRetryAsync<PricingDbContext>(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "pricing" }));
app.MapControllers();
app.Run();

