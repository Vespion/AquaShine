using BlazorStrap;
using MatBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazor.Extensions.Logging;
using Microsoft.Extensions.Logging;
using H3x.BlazorProgressIndicator;

namespace AquaShine.WebFacade
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddProgressIndicator();

            builder.Logging.ClearProviders();
            builder.Logging.AddBrowserConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            builder.Services.AddBootstrapCss();
            builder.Services.AddMatToaster();

            builder.Services.AddHttpClient("ApiClient", (sp, client) =>
            {
                client.BaseAddress = new Uri("http://localhost:7071/api/");
            });

            builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            await builder.Build()
                .RunAsync();
        }
    }
}