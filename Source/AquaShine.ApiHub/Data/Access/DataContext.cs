using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Models;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Queryable;
using Azure.Storage.Sas;
using CloudStorageAccount = Microsoft.Azure.Storage.CloudStorageAccount;

namespace AquaShine.ApiHub.Data.Access
{
    /// <summary>
    /// Wrapper for data operations
    /// </summary>
    public class DataContext
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudTable _entrantTable;

        /// <summary>
        /// Constructs the data context
        /// </summary>
        /// <param name="account"></param>
        /// <param name="storageAccount"></param>
        public DataContext(CloudTableClient account, CloudStorageAccount storageAccount)
        {
            if(account == null) throw new ArgumentNullException(nameof(account));
            _storageAccount = storageAccount ?? throw new ArgumentNullException(nameof(storageAccount));
            _entrantTable = account.GetTableReference("entrants");
        }

        /// <summary>
        /// Creates a new entrant in table storage
        /// </summary>
        /// <param name="entrant">The entrant to save. The ID properties must already be populated</param>
        /// <returns>A task that completes when the storage has been updated</returns>
        public async Task Create(Entrant entrant)
        {
            await _entrantTable.CreateIfNotExistsAsync().ConfigureAwait(false);
            var operation = TableOperation.Insert(entrant);
            await _entrantTable.ExecuteAsync(operation).ConfigureAwait(false);
        }


        /// <summary>
        /// Find an entrant by their row key
        /// </summary>
        /// <param name="entrantId"></param>
        /// <returns></returns>
        public async Task<Entrant?> FindById(string entrantId)
        {
            if (string.IsNullOrWhiteSpace(entrantId)) throw new ArgumentNullException(nameof(entrantId));
            if(!await _entrantTable.ExistsAsync().ConfigureAwait(false)) throw new DirectoryNotFoundException();
            var operation = TableOperation.Retrieve<Entrant>("A", entrantId);
            var opResult = await _entrantTable.ExecuteAsync(operation).ConfigureAwait(false);
            return (Entrant?) opResult.Result;
        }

        /// <summary>
        /// Fetches a list of entrants ordered by their submission times. This is an expensive process so results should be cached
        /// </summary>
        /// <param name="count">Number of entrants to get</param>
        /// <param name="skip">Skip forward this many entrants in the list</param>
        /// <param name="verified">If true show only verified submissions, false to show only unverified and null to show all</param>
        /// <returns></returns>
        public async Task<IEnumerable<Entrant>> FetchSubmissionOrderedList(int count, int skip, bool? verified)
        {
            var query = _entrantTable.CreateQuery<Entrant>()
                .Where(z => z.Details!.Locked);
            if (verified.HasValue)
            {
                query = query.Where(x => x.Details!.Verified == verified.Value);
            }

            TableQuerySegment<Entrant>? querySegment = null;
            var returnList = new List<Entrant>();

            while (querySegment == null || querySegment.ContinuationToken != null)
            {
                querySegment = await query.AsTableQuery()
                        .ExecuteSegmentedAsync(querySegment?.ContinuationToken).ConfigureAwait(false);
                    returnList.AddRange(querySegment);

            }

            return returnList.OrderBy(o => o.Details!.TimeToComplete).Skip(skip).Take(count);
        }

        /// <summary>
        /// Fetches a list of entrants with no ordering
        /// </summary>
        /// <param name="count">Number of entrants to fetch</param>
        /// <param name="skip">Skip forward this many entrants</param>
        /// <param name="verified">Optional parameter to filter by verification state</param>
        /// <returns></returns>
        public async Task<IEnumerable<Entrant>> FetchList(int count, int skip, bool? verified = null)
        {
            var query = _entrantTable.CreateQuery<Entrant>().AsQueryable();

            if (verified.HasValue)
            {
                query = query.Where(x => x.Details!.Verified == verified.Value);
            }

            TableQuerySegment<Entrant>? querySegment = null;
            var returnList = new List<Entrant>();

            while (querySegment == null || querySegment.ContinuationToken != null)
            {
                if (count == 0)
                {
                    return returnList;
                }

                querySegment = await query.AsTableQuery()
                    .ExecuteSegmentedAsync(querySegment?.ContinuationToken).ConfigureAwait(false);

                foreach (var querySegmentResult in querySegment.Results)
                {
                    if (skip > 0)
                    {
                        skip--;
                        continue;
                    }

                    if (count > 0)
                    {
                        count--;
                        returnList.Add(querySegmentResult);
                    }

                    if (count == 0)
                    {
                        return returnList;
                    }
                }
            }

            return returnList;
        }

        /// <summary>
        /// Fetches an entrant by their eventbrite ID
        /// </summary>
        /// <param name="entrantEventbriteId"></param>
        /// <returns></returns>
        public Task<Entrant?> FindByEventbriteId(string entrantEventbriteId)
        {
            if (entrantEventbriteId == null) throw new ArgumentNullException(nameof(entrantEventbriteId));

            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
            return Task.Run(() => _entrantTable.CreateQuery<Entrant>().Where(x => x.EventbriteId == entrantEventbriteId).FirstOrDefault())!;
        }

        /// <summary>
        /// Merges the entrant with the data on the store, will insert if the entity does not already exist
        /// </summary>
        /// <param name="entrant">The entrant to merge</param>
        /// <returns>The updated representation of the model</returns>
        public async Task<Entrant> MergeWithStore(Entrant entrant)
        {
            var operation = TableOperation.InsertOrMerge(entrant);
            return (Entrant) (await _entrantTable.ExecuteAsync(operation).ConfigureAwait(false)).Result;
        }

        /// <summary>
        /// Generates the URIs that a client can use to upload images to the storage system
        /// </summary>
        /// <param name="entrantId">The entrant that is being uploaded</param>
        /// <param name="generateDisplayUri">Controls the generation of the displayUploadUri</param>
        /// <param name="verifyContainerName">The name of the blob container for verification images. Default: 'photos-verification'</param>
        /// <param name="displayContainerName">The name of the blob container for verification images. Default: 'photos-display'</param>
        /// <returns></returns>
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
    }
}
