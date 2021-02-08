using AquaShine.ApiHub.Data.Access;
using AquaShine.WebSupport.Api.Submission;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace AquaShine.ApiFacade.Surface
{
    public class Submission
    {
        private readonly IDataContext _context;

        public Submission(IDataContext context)
        {
            _context = context;
        }

        [FunctionName("StartSubmission")]
        public async Task<IActionResult> StartSubmission(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "results/{entrantId}")]
            HttpRequest req, string entrantId,
            ILogger log)
        {
            var request = JsonSerializer.Deserialize<InitalRequest>(await req.ReadAsStringAsync());
            var entrant = await _context.FindById(entrantId);
            if (entrant == null)
            {
                return new NotFoundResult();
            }

            if (entrant.Submission?.Locked == true)
            {
                return new StatusCodeResult(StatusCodes.Status423Locked);
            }

            var uris = await _context.GenerateImageUploadUris(entrantId, request.GenerateDisplayImg);
            entrant.Submission = new ApiHub.Data.Models.Submission
            {
                DisplayImgUrl = uris.displayUploadUri,
                VerifyingImgUrl = uris.verifyUploadUri,
                DisplayName = request.DisplayName,
                Locked = false,
                Show = request.Public,
                TimeToComplete = request.TimeTaken,
                Verified = false
            };

            await _context.MergeWithStore(entrant);

            return new JsonResult(new InitalResponse
            {
                DisplayUri = uris.displayUploadUri,
                VerificationUri = uris.verifyUploadUri
            });
        }

        [FunctionName("LockSubmission")]
        public async Task<IActionResult> LockSubmission(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "results/{entrantId}/lock")]
            HttpRequest req, string entrantId,
            ILogger log)
        {
            var entrant = await _context.FindById(entrantId);
            if (entrant?.Submission == null)
            {
                return new NotFoundResult();
            }

            if (entrant.Submission.Locked)
            {
                return new StatusCodeResult(StatusCodes.Status423Locked);
            }

            entrant.Submission.Locked = true;

            await _context.MergeWithStore(entrant);

            return new OkResult();
        }
    }
}