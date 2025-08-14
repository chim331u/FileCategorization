using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FileCategorization_Web;
using FileCategorization_Web.Extensions;
using FileCategorization_Web.Interfaces;
using FileCategorization_Web.Services;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add logging (configuration is automatically loaded in WebAssembly)

// Add default HttpClient for other services
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add Radzen services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Add modern file categorization services with HttpClientFactory and Polly
builder.Services.AddFileCategorizationServices(builder.Configuration);

// Keep legacy service for WebScrum (will be modernized later)
builder.Services.AddScoped<IWebScrumServices, WebScrumServices>();

await builder.Build().RunAsync();