using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AquaShine.ApiHub.Data.Access
{
    public class DbDataContext : DbContext, IDataContext
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly ILogger<DbDataContext> _logger;
        public virtual DbSet<Entrant> Entrants { get; set; }

        internal virtual DbSet<Submission> Submissions { get; set; }

        /// <inheritdoc />
        public DbDataContext(DbContextOptions<DbDataContext> dbContext, CloudStorageAccount storageAccount, ILogger<DbDataContext> logger) : base(dbContext)
        {
            _storageAccount = storageAccount;
            _logger = logger;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Framework method")]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _logger.LogDebug("Constructing DB model");
            modelBuilder.Entity<Address>().Property<int>("Id");
            modelBuilder.Entity<Address>().HasKey("Id");
            modelBuilder.Entity<Address>().HasOne<Entrant>().WithOne(x => x.Address).HasForeignKey<Entrant>("AddressId");

            modelBuilder.Entity<Entrant>().HasKey(k => k.RowKey);
            modelBuilder.Entity<Entrant>().HasIndex(x => x.EventbriteId);
            //modelBuilder.Entity<Entrant>().HasOne<Submission>().WithOne();//.HasForeignKey(typeof(Submission), "EntrantId");

            modelBuilder.Entity<Submission>().Property<int>("Id");
            modelBuilder.Entity<Submission>().HasKey("Id");
            modelBuilder.Entity<Submission>().HasIndex(x => x.TimeToComplete);
            modelBuilder.Entity<Submission>().HasIndex(x => x.Verified);
            modelBuilder.Entity<Submission>().HasIndex(x => x.Locked);
            modelBuilder.Entity<Submission>().HasOne<Entrant>().WithOne().HasForeignKey(typeof(Entrant), "EntrantId");
            base.OnModelCreating(modelBuilder);
            _logger.LogDebug("DB model construction time");
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            _logger.LogDebug("Persisting changes to database");
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <inheritdoc />
        public async Task Create(Entrant entrant)
        {
            await Entrants.AddAsync(entrant).ConfigureAwait(false);
            await SaveChangesAsync();
        }

        /// <inheritdoc />
        public Task<IEnumerable<Entrant>> FetchList(int count, int skip, bool? verified = null)
        {
            if (verified.HasValue)
            {
                return Task.Run(() =>
                {
                    return Entrants.Where(x => x.Submission != null && x.Submission.Verified == verified && !x.Submission.Rejected)
                        .Skip(skip).Take(count)
                        .Include(y => y.Submission).AsEnumerable();
                });
            }

            return Task.Run(() => Entrants.Skip(skip).Take(count).AsEnumerable());
        }

        /// <inheritdoc />
        public Task<IEnumerable<Entrant>> FetchSubmissionOrderedList(int count, int skip, bool? verified)
        {
            return Task.Run(() =>
            {
                var query = Entrants.Where(x => x.Submission != null && x.Submission.Locked && !x.Submission.Rejected);
                if (verified.HasValue)
                {
                    query = query.Where(x => x.Submission != null && x.Submission.Verified == verified);
                }

                return query.OrderBy(t => t.Submission!.TimeToComplete)
                        .Skip(skip).Take(count)
                    .Include(y => y.Submission).AsEnumerable();
            });
        }

        public Task<IEnumerable<Entrant>> FetchByNameFilterOrdered(string name, int count, int skip, bool? verified)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return FetchSubmissionOrderedList(count, skip, verified);
            }
            return Task.Run(() =>
            {
                var query = Entrants.Where(x => x.Submission.Locked && !x.Submission.Rejected);
                if (verified.HasValue)
                {
                    query = query.Where(x => x.Submission.Verified == verified);
                }

                query = query.Where(s => s.Name.Contains(name));
                return query.OrderBy(t => t.Submission.TimeToComplete).Skip(skip).Take(count).AsEnumerable();
            });
        }

        /// <inheritdoc />
        public Task<Entrant?> FindByEventbriteId(string entrantEventbriteId)
        {
            return Entrants.FirstOrDefaultAsync(x => x.EventbriteId == entrantEventbriteId);
        }

        /// <inheritdoc />
        public Task<Entrant?> FindById(string entrantId)
        {
            return Entrants.FindAsync(entrantId).AsTask();
        }

        /// <inheritdoc />
        public async Task<(Uri verifyUploadUri, Uri? displayUploadUri)> GenerateImageUploadUris(string entrantId,
            bool generateDisplayUri, string verifyContainerName = "photos-verification",
            string displayContainerName = "photos-display")
        {
            if (string.IsNullOrWhiteSpace(entrantId)) throw new ArgumentNullException(nameof(entrantId));

            //Generate containers
            var blobClient = new BlobContainerClient(_storageAccount.ToString(true), displayContainerName);
            await blobClient.CreateIfNotExistsAsync(PublicAccessType.Blob).ConfigureAwait(false);
            blobClient = new BlobContainerClient(_storageAccount.ToString(true), verifyContainerName);
            await blobClient.CreateIfNotExistsAsync(PublicAccessType.None).ConfigureAwait(false);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = verifyContainerName,
                BlobName = entrantId,
                Resource = @"b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
            };

            //Sets the permissions for the SAS token
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write | BlobSasPermissions.Delete);

            var credentials = new StorageSharedKeyCredential(_storageAccount.Credentials.AccountName,
                _storageAccount.Credentials.ExportBase64EncodedKey());

            //The constructed sas token
            var verifyUploadSas = sasBuilder.ToSasQueryParameters(credentials);

            //Uri builder for verify blob
            var builder = new BlobUriBuilder(_storageAccount.BlobEndpoint)
            {
                BlobName = entrantId,
                BlobContainerName = verifyContainerName,
                AccountName = _storageAccount.Credentials.AccountName,
                Sas = verifyUploadSas
            };
            var verifyUploadUri = builder.ToUri();

            sasBuilder.BlobContainerName = displayContainerName;
            builder.BlobContainerName = displayContainerName;

            if (generateDisplayUri)
            {
                builder.Sas = sasBuilder.ToSasQueryParameters(credentials);
                var displayUploadUri = builder.ToUri();
                return (verifyUploadUri, displayUploadUri);
            }
            return (verifyUploadUri, null);
        }

        /// <inheritdoc />
        public async Task<Entrant> MergeWithStore(Entrant entrant)
        {
            Entrants.Update(entrant);
            await SaveChangesAsync().ConfigureAwait(false);
            return await Entrants.FindAsync(entrant.RowKey).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Delete(Entrant entrant)
        {
            Entrants.Remove(entrant);
            await SaveChangesAsync().ConfigureAwait(false);
        }

        public Task<int> GetTotalSubmissions(bool? verified)
        {
            if (verified.HasValue)
            {
                return Entrants.Where(x => x.Submission != null && x.Submission.Locked && x.Submission.Verified == verified).CountAsync();
            }
            return Entrants.Where(x => x.Submission != null && x.Submission.Locked).CountAsync();
        }
    }
}