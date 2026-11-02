
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => "Equipment Monitoring API is running");

app.MapControllers();

app.Run();
