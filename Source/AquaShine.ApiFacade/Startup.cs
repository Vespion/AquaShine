using System;
using AquaShine.ApiFacade;
using AquaShine.ApiHub.Data.Access;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite;
using AquaShine.ApiHub.Eventbrite.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
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
                configure.SetMinimumLevel(LogLevel.Trace);
                configure.AddConsole();
            });

            builder.Services.AddSingleton<Microsoft.Azure.Storage.CloudStorageAccount>(factory =>
            {
                var configuration = factory.GetService<IConfiguration>();
                return Microsoft.Azure.Storage.CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
            });

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
                BatchSize = 1,
                MaxWriteAttempts = 5,
            });
            builder.Services.AddHttpClient<ApiClient>();
            builder.Services.AddSingleton<ApiSerialiser>();

            builder.Services.AddScoped<IDataContext, DbDataContext>(sp =>
            {
                var _environment = sp.GetService<IHostingEnvironment>();
                var optionsBuilder = new DbContextOptionsBuilder<DbDataContext>()
                    .EnableDetailedErrors();
                if (_environment.IsDevelopment())
                {
                    optionsBuilder.EnableSensitiveDataLogging();
                }
                optionsBuilder.UseInMemoryDatabase("aqua-shine");

                var context = new DbDataContext(optionsBuilder.Options,
                    sp.GetRequiredService<Microsoft.Azure.Storage.CloudStorageAccount>());
                if (init)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        context.Entrants.Add(new Entrant
                        {
                            Email = "jonathon.spice@gmail.com",
                            Address = new Address
                            {
                                City = "Sheffield",
                                Region = "South Yorkshire",
                                PostalCode = "S61rt",
                                Address1 = "5 Eastgate",
                                Country = "GB"
                            },
                            BioGender = Gender.Female,
                            EventbriteId = "1428494963",
                            RowKey = i.ToString("X"),
                            PartitionKey = "A",
                            Name = "Jonathon Spice",
                            Submission = new Submission
                            {
                                Locked = true,
                                Show = true,
                                TimeToComplete = TimeSpan.FromHours(2.5 + i),
                                Verified = i % 2 != 0,
                                DisplayImgUrl = new Uri("https://th.bing.com/th/id/OIP.DFxl_HjAxACjCoF5yMRHeAAAAA?pid=Api&rs=1")
                            }
                        });
                    }

                    context.SaveChanges();
                    init = false;
                }
                return context;
            });
        }
    }
}
