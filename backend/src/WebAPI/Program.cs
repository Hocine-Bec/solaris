using Application;
using Hangfire;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Measurement;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using WebAPI.Json;
using WebAPI.Measurement;
using WebAPI.Security;
using WebAPI.Startup;

// Settings come from appsettings.json (non-secret defaults) and environment variables (secrets), which
// CreateBuilder reads; the app never loads a .env file itself (docs/operations.md, Configuration).
var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter()));

// Every endpoint needs a signed-in account unless it allows anonymous callers; policies narrow it to a role (ADR 9)
builder.Services.AddStaffPolicies();

// Every error response is a ProblemDetails (RFC 9457) with a stable code (ADR 3). An expected failure sets its own
// (ApiControllerBase.Problem); the others get one here: validation_failed, internal_error, or the status's name.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    var problem = context.ProblemDetails;
    if (problem.Extensions.ContainsKey("code")) return;
    var status = problem.Status ?? context.HttpContext.Response.StatusCode;
    problem.Extensions["code"] = problem switch
    {
        HttpValidationProblemDetails => "validation_failed",
        _ when status == StatusCodes.Status500InternalServerError => "internal_error",
        _ => ReasonPhrases.GetReasonPhrase(status) is { Length: > 0 } phrase
            ? phrase.ToLowerInvariant().Replace(' ', '_')
            : "error"
    };
});

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

    // Query parameters bind whatever their case; the reference names them in camelCase, like the JSON bodies
    // (?page=1&pageSize=20 rather than the C# property names Page and PageSize).
    options.AddOperationTransformer((operation, _, _) =>
    {
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.In == ParameterLocation.Query && parameter.Name is { } name)
                parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
        }
        return Task.CompletedTask;
    });
});

//Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The measurement harness (docs/measurements.md §4) exists in Development only.
if (builder.Environment.IsDevelopment())
    builder.Services.AddMeasurementHarness();

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

// Check every setting now, before the database is touched; app.Run() would only check them after the migrations.
app.Services.GetRequiredService<IStartupValidator>().Validate();

// One log line per request, with its final status. It comes first, so it sees the status the client gets and an
// exception reaches it already handled: the exception handler below logs it, once.
app.UseSerilogRequestLogging();

// Outside Development, an unhandled exception becomes a 500 ProblemDetails with a traceId and no stack trace; the
// exception is logged with the same trace id. In Development, the developer exception page shows the details.
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

// Responses that end with an error status and no body (an unknown URL, a wrong method) get a ProblemDetails body too.
app.UseStatusCodePages();

app.UseHangfireDashboard();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development only, so open to anyone there: the API reference loads without signing in
    app.MapOpenApi().AllowAnonymous();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Solar CRM API")
            .WithTheme(ScalarTheme.Kepler)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    }).AllowAnonymous();

    app.MapMeasurementEndpoints();
    
    // Auto-migrate database in development
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

// The first Admin, from the Admin settings, when no active Admin exists; the API stops if there is neither (FR-U3).
await FirstAdmin.EnsureAsync(app.Services);

app.UseHttpsRedirection();

app.UseCors();

// A request that changes data with the session cookie, or signs in or out, must come from the API's own site (ADR 7).
app.UseMiddleware<SameOriginCheck>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();