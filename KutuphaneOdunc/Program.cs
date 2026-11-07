using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using KutuphaneOdunc;
using KutuphaneOdunc.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<LibraryState>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<ToastService>();

await builder.Build().RunAsync();
