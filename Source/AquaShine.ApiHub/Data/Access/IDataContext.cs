using AquaShine.ApiHub.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AquaShine.ApiHub.Data.Access
{
    public interface IDataContext
    {
        /// <inheritdoc />
        Task Create(Entrant entrant);

        /// <inheritdoc />
        Task<IEnumerable<Entrant>> FetchList(int count, int skip, bool? verified = null);

        /// <inheritdoc />
        Task<IEnumerable<Entrant>> FetchSubmissionOrderedList(int count, int skip, bool? verified);

        Task<IEnumerable<Entrant>> FetchByNameFilterOrdered(string name, int count, int skip, bool? verified);

        /// <inheritdoc />
        Task<Entrant?> FindByEventbriteId(string entrantEventbriteId);

        /// <inheritdoc />
        Task<Entrant?> FindById(string entrantId);

        /// <inheritdoc />
        Task<(Uri verifyUploadUri, Uri? displayUploadUri)> GenerateImageUploadUris(string entrantId,
            bool generateDisplayUri, string verifyContainerName = "photos-verification",
            string displayContainerName = "photos-display");

        /// <inheritdoc />
        Task<Entrant> MergeWithStore(Entrant entrant);

        Task Delete(Entrant entrant);

        Task<int> GetTotalSubmissions(bool? verified);
    }
}