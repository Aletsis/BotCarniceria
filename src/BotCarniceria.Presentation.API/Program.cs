using BotCarniceria.Core;
using BotCarniceria.Infrastructure;
using BotCarniceria.Infrastructure.BackgroundJobs.Configuration;
using BotCarniceria.Application.Bot;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Presentation.API.Middleware;
using BotCarniceria.Presentation.API.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Add Services (Clean Architecture)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBotApplication();

// 3. Add API Services
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// 3b. Add Authentication (JWT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_must_be_very_long_for_security_default_12345";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BotCarniceriaAPI";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BotCarniceriaClient";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// 4. Configure CORS with restricted policy
builder.Services.AddRestrictedCors(builder.Configuration);

// Add Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddFixedWindowLimiter("WebhookPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

// Register HttpRealTimeNotificationService for API to notify Blazor
builder.Services.AddHttpClient();
builder.Services.AddScoped<BotCarniceria.Core.Application.Interfaces.IRealTimeNotificationService, BotCarniceria.Presentation.API.Services.HttpRealTimeNotificationService>();

builder.Services.AddHostedService<BotCarniceria.Presentation.API.Services.SessionTimeoutBackgroundService>();

var app = builder.Build();

// 4. Configure Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Auto-migrate in Development for convenience
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BotCarniceriaDbContext>();
        try 
        {
            dbContext.Database.Migrate();
            Log.Information("Database migrated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database.");
        }
    }
}

// IMPORTANT: Add signature validation middleware BEFORE routing
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<WhatsAppSignatureValidationMiddleware>();
}

// Apply CORS policy
app.UseCors(CorsServiceExtensions.GetPolicyName());

// Configure Hangfire Dashboard
var hangfireOptions = builder.Configuration
    .GetSection(HangfireOptions.SectionName)
    .Get<HangfireOptions>() ?? new HangfireOptions();
app.UseHangfireDashboardWithAuth(hangfireOptions);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.UseRateLimiter();
app.MapControllers();

try 
{
    Log.Information("Starting Web API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
