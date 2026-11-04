using Application;
using DotNetEnv;
using Hangfire;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

Env.Load();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers();

// Auto-validation using FluentValidation 
builder.Services.AddFluentValidationAutoValidation();

// OpenAPI document for the API reference (mapped in Development only)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Solar CRM API";
        document.Info.Description = "API for managing solar installations, customers, and energy monitoring";
        return Task.CompletedTask;
    });
});

//Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHangfireDashboard();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Solar CRM API")
            .WithTheme(ScalarTheme.Kepler)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
    
    // Auto-migrate database in development
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await new DatabaseSeeder(context).SeedAsync();
}


app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();