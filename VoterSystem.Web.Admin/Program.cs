using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VoterSystem.Web.Admin;
using VoterSystem.Shared.Blazor.Infrastructure;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSharedBlazorServices(builder.Configuration);

var app = builder.Build();

await app.RunAsync();