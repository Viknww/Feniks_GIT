using Feniks.Web;
using Feniks.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5050") });

// Существующие сервисы
builder.Services.AddScoped<ObjectsService>();
builder.Services.AddScoped<ReferenceCategoryService>();
builder.Services.AddScoped<RefCatalogService>();
builder.Services.AddScoped<ReferenceService>();
builder.Services.AddScoped<ReferenceItemService>();

// Обновленные сервисы
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DataSyncService>(); // НОВЫЙ сервис

await builder.Build().RunAsync();