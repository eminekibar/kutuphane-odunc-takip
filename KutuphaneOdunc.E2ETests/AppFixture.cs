using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace KutuphaneOdunc.E2ETests;

// Bütün uçtan uca testler için bir kez çalışır:
// 1. Uygulamayı geçici bir klasöre publish eder.
// 2. Publish çıktısını testin içinde başlatılan küçük bir web sunucusuyla sunar.
// 3. Playwright'ın Chromium tarayıcısını kurar ve başlatır.
public sealed class AppFixture : IAsyncLifetime
{
    private WebApplication? _server;
    private IPlaywright? _playwright;
    private string _publishDir = "";

    public string BaseUrl { get; private set; } = "";
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Assertions.SetDefaultExpectTimeout(15_000);

        var root = FindRepositoryRoot();
        _publishDir = Path.Combine(Path.GetTempPath(), "kutuphane-e2e-" + Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "KutuphaneOdunc", "KutuphaneOdunc.csproj");
        await RunAsync("dotnet", $"publish \"{project}\" -c Release -o \"{_publishDir}\"", root);

        await StartServerAsync(Path.Combine(_publishDir, "wwwroot"));

        // İlk çalıştırmada Chromium'u indirir (yaklaşık 150 MB), sonraki çalıştırmalarda hiçbir şey yapmaz.
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
            throw new InvalidOperationException("Playwright Chromium kurulamadı.");

        _playwright = await Playwright.CreateAsync();

        // HEADED=1 ortam değişkeniyle tarayıcı görünür ve yavaş çalışır (testleri izlemek için).
        var headed = Environment.GetEnvironmentVariable("HEADED") == "1";
        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = !headed,
            SlowMo = headed ? 300 : 0
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.CloseAsync();
        _playwright?.Dispose();

        if (_server is not null)
            await _server.StopAsync();

        try { Directory.Delete(_publishDir, recursive: true); } catch { /* geçici klasör, silinemezse sorun değil */ }
    }

    private async Task StartServerAsync(string webRoot)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = webRoot });
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // 0: boş bir port seçilsin
        builder.Logging.ClearProviders();

        _server = builder.Build();
        _server.UseDefaultFiles();
        _server.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
        _server.MapFallbackToFile("index.html"); // /books/B001 gibi adresler de index.html'i alsın
        await _server.StartAsync();

        var addresses = _server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        BaseUrl = addresses.Addresses.First();
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KutuphaneOdunc.sln")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new DirectoryNotFoundException("KutuphaneOdunc.sln bulunamadı.");
    }

    private static async Task RunAsync(string fileName, string arguments, string workingDirectory)
    {
        var info = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"'{fileName} {arguments}' başarısız oldu:\n{await output}\n{await error}");
    }
}

[CollectionDefinition(Name)]
public class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "Uygulama";
}
