using System;
using System.IO;
using System.Threading.Tasks;
using AquaShine.ApiHub.Data.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AquaShine.ApiFacade.Surface
{
    public class Admin
    {
        private readonly IDataContext _context;

        public Admin(IDataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifies that an administration key is correct. Mostly used for UI validation
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("CommitteeKeyCheck")]
        public Task<IActionResult> VerifyAdminKey(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "committee/keycheck")]
            HttpRequest req,
            ILogger log)
        {
            return Task.FromResult<IActionResult>(new OkResult());
        }

        /// <summary>
        /// Marks a submission as verified
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("MarkSubmissionVerified")]
        public async Task<IActionResult> MarkVerified(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "results/{entrantId}/verify")]
            HttpRequest req, string entrantId,
            ILogger log)
        {
            var entrant = await _context.FindById(entrantId);
            if (entrant?.Submission == null)
            {
                return new NotFoundResult();
            }

            if (!entrant.Submission.Locked)
            {
                return new StatusCodeResult(StatusCodes.Status422UnprocessableEntity);
            }

            entrant.Submission.Verified = true;

            if (bool.TryParse(req.Query["hide"], out var hide))
            {
                if (hide)
                {
                    entrant.Submission.Show = false;
                }
            }

            await _context.MergeWithStore(entrant);

            return new OkResult();
        }

        /// <summary>
        /// Marks a submission as rejected
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("MarkSubmissionRejected")]
        public async Task<IActionResult> MarkRejected(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "results/{entrantId}/reject")]
            HttpRequest req, string entrantId,
            ILogger log)
        {
            var entrant = await _context.FindById(entrantId);
            if (entrant?.Submission == null)
            {
                return new NotFoundResult();
            }

            if (!entrant.Submission.Locked)
            {
                return new StatusCodeResult(StatusCodes.Status422UnprocessableEntity);
            }

            entrant.Submission.Rejected = true;

            await _context.MergeWithStore(entrant);

            return new OkResult();
        }
    }
}
