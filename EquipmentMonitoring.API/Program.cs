
using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.API.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EquipmentMonitoring.Application.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "EquipmentMonitoring.API.xml"));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "EquipmentMonitoring.Application.xml"));
    options.OperationFilter<EquipmentExamplesOperationFilter>();
    options.SchemaFilter<EquipmentExamplesOperationFilter>();
    options.OperationFilter<MeasurementExamplesOperationFilter>();
    options.SchemaFilter<MeasurementExamplesOperationFilter>();
    options.OperationFilter<AlertExamplesOperationFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("EquipmentDatabase")
    ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "equipmentmonitoring.db")}";
builder.Services.AddDbContext<EquipmentDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IEquipmentSerialNumberChecker, EquipmentSerialNumberChecker>();
builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
builder.Services.AddScoped<IThresholdRepository, ThresholdRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
builder.Services.AddScoped<IEquipmentRegistrationService, EquipmentRegistrationService>();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();
builder.Services.AddScoped<IMeasurementAlertService, MeasurementAlertService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IValidator<CreateEquipmentDto>, CreateEquipmentDtoValidator>();
builder.Services.AddScoped<IValidator<CreateMeasurementDto>, CreateMeasurementDtoValidator>();
builder.Services.AddScoped<IValidator<CreateMaintenanceDto>, CreateMaintenanceDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateMaintenanceDto>, UpdateMaintenanceDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateEquipmentDto>, UpdateEquipmentDtoValidator>();

var app = builder.Build();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API test endpoint
app.MapGet("/", () => "Equipment Monitoring API is running");

app.MapControllers();

app.Run();

public partial class Program { }
