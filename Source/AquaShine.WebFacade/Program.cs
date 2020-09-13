using BlazorStrap;
using MatBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace AquaShine.WebFacade
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddLoadingBar();
            builder.Services.AddLogging();
            builder.Services.AddBootstrapCss();
            builder.Services.AddMatToaster();

            builder.Services.AddHttpClient("ApiClient", (sp, client) =>
            {
                client.BaseAddress = new Uri("");
                client.EnableIntercept(sp);
            });

            await builder.Build()
                .UseLoadingBar()
                .RunAsync();
        }
    }
}