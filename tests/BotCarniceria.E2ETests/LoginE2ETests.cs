using Microsoft.Playwright;
using BotCarniceria.E2ETests.Infrastructure;
using FluentAssertions;
using Xunit;
using System.Text.RegularExpressions;

namespace BotCarniceria.E2ETests;

public class LoginE2ETests : IClassFixture<AppFixture>
{
    private readonly AppFixture _fixture;

    public LoginE2ETests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldAccessProtectedContent()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        // 1. Go to Login manually (since Home is public)
        var loginUrl = _fixture.ServerAddress + "/login";
        await page.GotoAsync(loginUrl);
        await Assertions.Expect(page).ToHaveURLAsync(loginUrl);
        
        // 2. Fill Form
        await page.FillAsync("input[name='username']", "admin");
        await page.FillAsync("input[name='password']", "Admin123!");
        
        // 3. Click Submit
        await page.ClickAsync("button[type='submit']");
        
        // 4. Expect Redirect to Home
        await Assertions.Expect(page).ToHaveURLAsync(_fixture.ServerAddress + "/", new() { Timeout = 30000 });
        
        // 5. Navigate to Protected Page (Pedidos)
        await page.GotoAsync(_fixture.ServerAddress + "/pedidos");
        
        // 6. Verify Content
        // We expect to verify we are authorized.
        // If authorized, we see "Gestión de Pedidos" and NOT the error alert.
        await Assertions.Expect(page.GetByText("Gestión de Pedidos")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("No tienes permisos para ver esta página.")).Not.ToBeVisibleAsync();
    }
}
