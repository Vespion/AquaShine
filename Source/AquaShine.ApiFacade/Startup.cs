using System;
using AquaShine.ApiFacade;
using AquaShine.ApiFacade.Helpers;
using AquaShine.ApiHub.Data.Access;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite;
using AquaShine.ApiHub.Eventbrite.Models;
using AquaShine.Emails.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using SnowMaker;

[assembly: WebJobsStartup(typeof(Startup))]
namespace AquaShine.ApiFacade
{
    public class Startup : IWebJobsStartup
    {
        private static bool init = true;

        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddLogging(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Debug);
                configure.AddConsole();
            });

            var localRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            var azureRoot = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";

            var actualRoot = localRoot ?? azureRoot;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(actualRoot)
                .AddEnvironmentVariables()
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
            IConfiguration configuration = configBuilder.Build();

            MailChemist.MailChemist.RegisterGlobalTypes();
            MailChemist.MailChemist.RegisterGlobalFilters();

            builder.Services.AddSingleton<Microsoft.Azure.Storage.CloudStorageAccount>(factory => Microsoft.Azure.Storage.CloudStorageAccount.Parse(configuration["StorageConnectionString"]));

            builder.Services.AddTransient<Microsoft.Azure.Cosmos.Table.CloudTableClient>(factory =>
            {
                var tables = factory.GetRequiredService<Microsoft.Azure.Storage.CloudStorageAccount>();
                return new Microsoft.Azure.Cosmos.Table.CloudTableClient(tables.TableStorageUri.PrimaryUri, new StorageCredentials(tables.Credentials.AccountName,
                    tables.Credentials.ExportBase64EncodedKey(),
                    tables.Credentials.KeyName));
            });

            builder.Services.AddSingleton<IUniqueIdGenerator, UniqueIdGenerator>(sp => new UniqueIdGenerator(
                new BlobOptimisticDataStore(sp.GetRequiredService<Microsoft.Azure.Storage.CloudStorageAccount>(), "support"))
            {
                BatchSize = configuration.GetValue<int>("IdGen:BatchSize"),
                MaxWriteAttempts = configuration.GetValue<int>("IdGen:MaxWriteAttempts"),
            });
            
            builder.Services.AddHttpClient<ApiClient>();
            builder.Services.AddSingleton<ApiSerialiser>();
                
            builder.Services.Configure<EmailConfig>(configuration.GetSection("Email"));

            builder.Services.AddScoped<IMailClient, SmtpRelayClient>();

            builder.Services.AddDbContext<DbDataContext>(optionsBuilder =>
            {
                optionsBuilder.EnableDetailedErrors();
                //if (_environment.IsDevelopment())
                //{
                //    optionsBuilder.EnableSensitiveDataLogging();
                //}
                optionsBuilder.UseLazyLoadingProxies();
                optionsBuilder.UseNpgsql(configuration.GetConnectionStringOrSetting("MainDb"), provider =>
                {
                    provider.MigrationsAssembly(typeof(DbDataContext).Assembly.FullName);
                });
            });

            builder.Services.AddScoped<IDataContext>(sp =>
            {
                var context = sp.GetRequiredService<DbDataContext>();
                //if (init)
                //{
                //    context.Database.EnsureDeleted();
                //    context.Database.EnsureCreated();
                //    for (int i = 0; i < 2; i++)
                //    {
                //        context.Entrants.Add(new Entrant
                //        {
                //            Email = "jonathon.spice@gmail.com",
                //            Address = new Address
                //            {
                //                City = "Sheffield",
                //                Region = "South Yorkshire",
                //                PostalCode = "S61rt",
                //                Address1 = "5 Eastgate",
                //                Country = "GB"
                //            },
                //            BioGender = Gender.Female,
                //            EventbriteId = "1428494963",
                //            RowKey = i.ToString("X"),
                //            PartitionKey = "A",
                //            Name = "Jonathon Spice",
                //        });
                //    }

                //    context.SaveChanges();
                //    init = false;
                //}
                return context;
            });

        }
    }
}
