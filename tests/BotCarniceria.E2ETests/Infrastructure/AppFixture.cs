using System.Diagnostics;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;

namespace BotCarniceria.E2ETests.Infrastructure;

public class AppFixture : IDisposable
{
    private Process? _process;
    private readonly System.Text.StringBuilder _outputBuffer = new();
    private readonly object _lock = new();

    public string ServerAddress => "http://localhost:5111";

    public AppFixture()
    {
        var binPath = AppContext.BaseDirectory;
        
        var rootPath = Path.GetFullPath(Path.Combine(binPath, "../../../../.."));
        if (!File.Exists(Path.Combine(rootPath, "BotCarniceria.sln")))
        {
             // Fallback for different test runner paths
             rootPath = Path.GetFullPath(Path.Combine(binPath, "../../../.."));
        }
        
        var projectPath = Path.Combine(rootPath, "src/BotCarniceria.Presentation.Blazor");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            // Removed --no-build to ensure we have a fresh build if needed, though slower.
            // Added explicit environment config to ensure no surprises.
            Arguments = $"run --urls {ServerAddress}", 
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ConnectionStrings__DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=BotCarniceriaE2E;Trusted_Connection=True;MultipleActiveResultSets=true";

        // Cleanup DB before starting
        try 
        {
            var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BotCarniceria.Infrastructure.Persistence.Context.BotCarniceriaDbContext>();
            optionsBuilder.UseSqlServer(startInfo.Environment["ConnectionStrings__DefaultConnection"]);
            using var context = new BotCarniceria.Infrastructure.Persistence.Context.BotCarniceriaDbContext(optionsBuilder.Options, null!);
            context.Database.EnsureDeleted();
            Console.WriteLine("Dropped E2E Database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to drop database: {ex.Message}");
        }

        Console.WriteLine($"Starting app at {projectPath}...");
        _process = new Process { StartInfo = startInfo };
        
        _process.OutputDataReceived += (sender, e) => 
        {
            if (e.Data != null)
            {
                lock (_lock) _outputBuffer.AppendLine($"[OUT]: {e.Data}");
                Console.WriteLine($"[APP]: {e.Data}");
            }
        };
        _process.ErrorDataReceived += (sender, e) => 
        {
            if (e.Data != null)
            {
                lock (_lock) _outputBuffer.AppendLine($"[ERR]: {e.Data}");
                Console.WriteLine($"[APP-ERR]: {e.Data}");
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        if (_process.HasExited)
        {
             throw new Exception($"Process exited immediately. Output: {_outputBuffer}");
        }

        WaitForServer().Wait();
    }

    private async Task WaitForServer()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2);
        var retries = 60; // Increase to 60 seconds
        
        while (retries > 0)
        {
            if (_process!.HasExited)
            {
                 throw new Exception($"Server process exited unexpectedly. Output:\n{_outputBuffer}");
            }

            try
            {
                var response = await client.GetAsync(ServerAddress);
                return;
            }
            catch
            {
                // Ignore connection errors
            }
            await Task.Delay(1000);
            retries--;
        }

        throw new Exception($"Server failed to start within timeout. Output captured:\n{_outputBuffer}");
    }

    public void Dispose()
    {
        if (_process != null && !_process.HasExited)
        {
            try { _process.Kill(true); } catch {}
            _process.Dispose();
        }
    }
}
