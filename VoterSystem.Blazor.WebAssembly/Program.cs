using ELTE.Cinema.Blazor.WebAssembly;
using ELTE.Cinema.Blazor.WebAssembly.Infrastructure;
using ELTE.Cinema.Blazor.WebAssembly.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorServices(builder.Configuration);

var app = builder.Build();

await app.RunAsync();
