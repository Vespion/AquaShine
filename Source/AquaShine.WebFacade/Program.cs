#define AutoDebugFlag

using BlazorStrap;
using MatBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using AquaShine.WebFacade.Helpers;
using Blazor.Extensions.Logging;
using Microsoft.Extensions.Logging;
using H3x.BlazorProgressIndicator;
using Microsoft.AspNetCore.Components.Authorization;

namespace AquaShine.WebFacade
{
    public class Program
    {
#if DEBUG && AutoDebugFlag
        public const bool DebugFlag = true;
#elif !DEBUG && AutoDebugFlag
        public const bool DebugFlag = false;
#else
//Manually set flag
        public const bool DebugFlag = false;
#endif
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddProgressIndicator();

            builder.Logging.ClearProviders();
            builder.Logging.AddBrowserConsole();
            if (!DebugFlag)
            {
#pragma warning disable CS0162 // Unreachable code detected
                //TODO Fix this
                //builder.Logging.AddApplicationInsights("bde3f897-6569-49dc-a7fe-2007d7ff5e5c");
#pragma warning restore CS0162 // Unreachable code detected
            }
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            builder.Services.AddBootstrapCss();
            builder.Services.AddMatToaster();

            builder.Services.AddHttpClient("ApiClient", (sp, client) =>
            {
                client.BaseAddress = new Uri(DebugFlag ? "http://localhost:7071/api/" : "https://aquashine-apifacade.azurewebsites.net/api/");
            });

            builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            builder.Services.AddScoped<CustomStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomStateProvider>();
            builder.Services.AddOptions();
            builder.Services.AddAuthorizationCore(options => { });


            await builder.Build()
                .RunAsync();
        }
    }
}