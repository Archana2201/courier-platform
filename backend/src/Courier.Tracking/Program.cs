using Courier.BuildingBlocks;
using Courier.Tracking.Data;
using Courier.Tracking.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrackingDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<TrackingEventHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await DatabaseStartup.EnsureCreatedWithRetryAsync<TrackingDbContext>(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "tracking" }));
app.MapControllers();
app.Run();
