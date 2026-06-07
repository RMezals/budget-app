using BudgetApp.Api.Configuration;
using BudgetApp.Api.Middleware;
using BudgetApp.Api.Modules.Auth;
using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Api.Modules.Dev;
using BudgetApp.Api.Modules.Portfolio.Repositories;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Savings.Services;
using BudgetApp.Api.Modules.Transactions.Repositories;
using BudgetApp.Api.Modules.Transactions.Services;
using BudgetApp.Api.Modules.Reports.Services;
using FirebaseAdmin;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// MongoDB configuration with validation
builder.Services.AddOptions<MongoDbSettings>()
    .Bind(builder.Configuration.GetSection("MongoDB"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return sp.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName);
});

// Firebase Admin SDK
var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"];
if (!string.IsNullOrEmpty(serviceAccountPath) && File.Exists(serviceAccountPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(serviceAccountPath)
    });
}

// HTTP client (for AI advisors)
builder.Services.AddHttpClient();

// Auth
builder.Services.AddScoped<IAuthService, FirebaseAuthService>();

// Repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<ILiabilityRepository, LiabilityRepository>();
builder.Services.AddScoped<ISavingsGoalRepository, SavingsGoalRepository>();
builder.Services.AddScoped<IGoalContributionRepository, GoalContributionRepository>();

// Services
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<ISavingsService, SavingsService>();
builder.Services.AddScoped<ISavingsProgressService, SavingsProgressService>();
builder.Services.AddScoped<BudgetApp.Api.Modules.Savings.Services.IGoalProjectionCalculator, BudgetApp.Api.Modules.Savings.Services.GoalProjectionCalculator>();
builder.Services.AddScoped<BudgetApp.Api.Modules.Dashboard.Services.IGoalProjectionCalculator, BudgetApp.Api.Modules.Dashboard.Services.GoalProjectionCalculator>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISpendingTrendService, SpendingTrendService>();
builder.Services.AddScoped<IAdvisorService, AdvisorService>();
builder.Services.AddScoped<IMonthlyReportService, MonthlyReportService>();
builder.Services.AddScoped<ISeedService, SeedService>();

// AI advisor providers
builder.Services.AddKeyedSingleton<IAiAdvisor, ClaudeAdvisor>(BudgetApp.Api.Modules.Dashboard.AiProviders.Claude);
builder.Services.AddKeyedSingleton<IAiAdvisor, OllamaAdvisor>(BudgetApp.Api.Modules.Dashboard.AiProviders.Ollama);
builder.Services.AddScoped<IAiAdvisorFactory, AiAdvisorFactory>();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste your Firebase ID token here (without the 'Bearer ' prefix).",
    });
    c.OperationFilter<BudgetApp.Api.Configuration.BearerSecurityOperationFilter>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Dev", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:5174")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Dev");
}
else
{
    // Enable HTTPS redirection in production
    app.UseHttpsRedirection();
}

// Global exception handler should be first in the pipeline
app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.MapControllers();
app.Run();
