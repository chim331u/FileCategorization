using System.Reflection;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Domain.Entities.Identity;
using FileCategorization_Api.Endpoints;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Services;
using FileCategorization_Api.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.AddApplicationServices();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();

builder.Services.AddHangfire(config =>
{
    config.
        UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage();
});
builder.Services.AddHangfireServer(options => options.SchedulePollingInterval = TimeSpan.FromSeconds(1));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo { Title = "Minimal API", Version = "v1", Description = "One App minimal API" });

    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("*")
            .AllowAnyMethod()
            .AllowAnyHeader()
            //.AllowCredentials()
            ;
    });
});

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);


var app = builder.Build();

 //Appy migrations on app start
 using (var scope = app.Services.CreateScope())
 {
     var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
     db.Database.Migrate();
 }

// // For Identity
// builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//     .AddEntityFrameworkStores<ApplicationContext>()
//     .AddDefaultTokenProviders();

app.UseCors("CorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); //it is new line
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapHub<NotificationHub>("notifications");

app.MapGroup("/api/v1/")
    .WithTags(" Files Detail endpoints")
    .MapFilesDetailEndPoint();

app.MapGroup("/api/v1/")
    .WithTags(" Configs endpoints")
    .MapConfigsEndPoint();

app.MapGroup("/api/v1/")
    .WithTags(" Actions endpoints")
    .MapActionsEndPoint();

app.MapGroup("/api/v1/")
    .WithTags(" DD endpoints")
    .MapDDEndPoint();

app.MapGroup("/api/v1/")
    .WithTags(" Utility endpoints")
    .MapUtilitiesEndPoint();

app.MapGroup("/api/v1/")
    .WithTags(" Identity endpoints")
    .MapIdentityEndPoint();

// Modern v2 endpoints with Repository Pattern and Result Pattern
app.MapGroup("/api/v2/")
    .WithTags("Files Query v2 (Repository Pattern)")
    .MapFilesQueryEndPoints();

app.MapGroup("/api/v2/")
    .WithTags("Files Management v2 (Repository Pattern)")
    .MapFilesManagementEndPoints();

app.MapGroup("/api/v2/")
    .WithTags("Utilities v2 (Repository Pattern)")
    .MapUtilityEndPoints();

app.MapGroup("/api/v2/")
    .WithTags("Configuration v2 (Repository Pattern)")
    .MapConfigEndPoints();

app.MapActionsV2Endpoints();

// DD v2 endpoints
app.MapDDEndpointsV2();

app.Run();

// Make Program class public for testing
public partial class Program { }