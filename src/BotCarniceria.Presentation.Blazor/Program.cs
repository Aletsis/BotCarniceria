using BotCarniceria.Application.Bot;
using BotCarniceria.Core;
using BotCarniceria.Infrastructure;
using BotCarniceria.Presentation.Blazor.Components;
using MudBlazor.Services;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Controllers for AccountController
builder.Services.AddControllers();

// Add Auth services
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add SignalR
builder.Services.AddSignalR();

// Add Clean Architecture Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBotApplication();
builder.Services.AddScoped<BotCarniceria.Core.Application.Interfaces.IRealTimeNotificationService, BotCarniceria.Presentation.Blazor.Services.SignalRNotificationService>();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BotCarniceria.Infrastructure.Persistence.Context.BotCarniceriaDbContext>();
    await BotCarniceria.Infrastructure.Persistence.DbInitializer.InitializeAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapHub<BotCarniceria.Presentation.Blazor.Hubs.ChatHub>("/chathub");

app.Run();
