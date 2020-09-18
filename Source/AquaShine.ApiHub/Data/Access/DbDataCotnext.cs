﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Models;
using AquaShine.ApiHub.Eventbrite.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AquaShine.ApiHub.Data.Access
{
    public class DbDataContext : DbContext, IDataContext
    {
        private readonly CloudStorageAccount _storageAccount;
        public virtual DbSet<Entrant> Entrants { get; set; }

        internal virtual DbSet<Submission> Submissions { get; set; }

        /// <inheritdoc />
        public DbDataContext(DbContextOptions<DbDataContext> dbContext, CloudStorageAccount storageAccount) : base(dbContext)
        {
            _storageAccount = storageAccount;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Framework method")]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>().Property<int>("Id");
            modelBuilder.Entity<Address>().HasKey("Id");

            modelBuilder.Entity<Entrant>().HasKey(k => k.RowKey);
            modelBuilder.Entity<Entrant>().HasIndex(x => x.EventbriteId);
            modelBuilder.Entity<Entrant>().HasOne<Submission>().WithOne().HasForeignKey(typeof(Submission), "SubmissionId");

            modelBuilder.Entity<Submission>().Property<int>("Id");
            modelBuilder.Entity<Submission>().HasKey("Id");
            modelBuilder.Entity<Submission>().HasIndex(x => x.TimeToComplete);
            modelBuilder.Entity<Submission>().HasIndex(x => x.Verified);
            modelBuilder.Entity<Submission>().HasIndex(x => x.Locked);
            base.OnModelCreating(modelBuilder);
        }

        /// <inheritdoc />
        public async Task Create(Entrant entrant)
        {
            await Entrants.AddAsync(entrant).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Entrant>> FetchList(int count, int skip, bool? verified = null)
        {
            if (verified.HasValue)
            {
                return Task.Run(() =>
                {
                    return Entrants.Where(x => x.Submission != null && x.Submission.Verified == verified)
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
                var query = Entrants.Where(x => x.Submission != null && x.Submission.Locked);
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
                var query = Entrants.Where(x => x.Submission.Locked);
                if (verified.HasValue)
                {
                    query = query.Where(x => x.Submission.Verified == verified);
                }

                query = query.Where(s => s.Name.Contains(name));
                return query.OrderBy(t => t.Submission.TimeToComplete).Skip(skip).Take(count)
                    .Include(y => y.Submission).AsEnumerable();
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
        public (Uri verifyUploadUri, Uri? displayUploadUri) GenerateImageUploadUris(string entrantId, bool generateDisplayUri, string verifyContainerName = "photos-verification", string displayContainerName = "photos-display")
        {
            if (string.IsNullOrWhiteSpace(entrantId)) throw new ArgumentNullException(nameof(entrantId));

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = verifyContainerName,
                BlobName = entrantId,
                Resource = @"b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
            };

            //Sets the permissions for the SAS token
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

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

        public Task<int> GetTotalSubmissions()
        {
            return Entrants.Where(x => x.Submission != null && x.Submission.Locked).CountAsync();
        }
    }
}