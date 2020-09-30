using System;
using System.Collections.Generic;
using System.Text;
using AquaShine.ApiHub.Data.Access;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AquaShine.ApiFacade.Helpers
{
    public class DesignTimeDbContext : IDesignTimeDbContextFactory<DbDataContext>
    {
        public DbDataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DbDataContext>();

            optionsBuilder.UseNpgsql("Server=aqua-shine.postgres.database.azure.com;Database=PrimaryAquaShine;Port=5432;User Id=super@aqua-shine;Password=T#C>n$H4|8>=F7lrBH>S;Ssl Mode=Require;", provider =>
            {
                provider.MigrationsAssembly("AquaShine.ApiHub");
            });

            return new DbDataContext(optionsBuilder.Options, 
                Microsoft.Azure.Storage.CloudStorageAccount.Parse("AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                                                                  "DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;" +
                                                                  "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"),
                NullLogger<DbDataContext>.Instance);
        }
    }
}
