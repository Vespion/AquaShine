using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Queryable;

namespace AquaShine.ApiHub.Data.Access
{
    /// <summary>
    /// Wrapper for data operations
    /// </summary>
    public class DataContext
    {
        private readonly CloudTable entrantTable;

        /// <summary>
        /// Constructs the data context
        /// </summary>
        /// <param name="account"></param>
        public DataContext(CloudTableClient account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            entrantTable = account.GetTableReference("entrants");
            
        }

        /// <summary>
        /// Creates a new entrant in table storage
        /// </summary>
        /// <param name="entrant">The entrant to save. The ID properties must already be populated</param>
        /// <returns>A task that completes when the storage has been updated</returns>
        public async Task Create(Entrant entrant)
        {
            await entrantTable.CreateIfNotExistsAsync().ConfigureAwait(false);
            var operation = TableOperation.Insert(entrant);
            await entrantTable.ExecuteAsync(operation).ConfigureAwait(false);
        }


        /// <summary>
        /// Find an entrant by their row key
        /// </summary>
        /// <param name="entrantId"></param>
        /// <returns></returns>
        public async Task<Entrant?> FindById(string entrantId)
        {
            if (string.IsNullOrWhiteSpace(entrantId)) throw new ArgumentNullException(nameof(entrantId));
            if(!await entrantTable.ExistsAsync().ConfigureAwait(false)) throw new DirectoryNotFoundException();
            var operation = TableOperation.Retrieve<Entrant>("A", entrantId);
            var opResult = await entrantTable.ExecuteAsync(operation).ConfigureAwait(false);
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
            var query = entrantTable.CreateQuery<Entrant>()
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
            var query = entrantTable.CreateQuery<Entrant>().AsQueryable();

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
            return Task.Run(() => entrantTable.CreateQuery<Entrant>().Where(x => x.EventbriteId == entrantEventbriteId).FirstOrDefault())!;
        }
    }
}
