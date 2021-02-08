using AquaShine.ApiFacade.Helpers;
using AquaShine.ApiHub.Data.Access;
using AquaShine.ApiHub.Data.Models;
using AquaShine.WebSupport.Api.DataTable;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AquaShine.ApiFacade.Surface
{
    public class DataQuery
    {
        private readonly IDataContext _context;

        public DataQuery(IDataContext context)
        {
            _context = context;
        }

        //[FunctionName("DataDump")]
        //public async Task<IActionResult> Dump(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dump")] HttpRequest req,
        //    ILogger log)
        //{
        //    //SearchTerm= (Filter I Think?)
        //    //Descending=False (sort order)
        //    //SortBy=
        //    //Page=1
        //    //PageSize=5
        //    var count = 0;
        //    var data = new List<Entrant>();
        //    while (true)
        //    {
        //        var result = (await _context.FetchByNameFilterOrdered(req.Query["SearchTerm"], 15, count, null)).ToArray();
        //        data.AddRange(result);
        //        count += result.Length;
        //        if (result.Length != 15)
        //        {
        //            break;
        //        }
        //    }
        //    var result2 = data.Select(e => e.ConvertToWebEntrant()).ToArray();
        //    var posCounter = 1;
        //    foreach (var entrant in result2)
        //    {
        //        entrant.Submission!.Position = posCounter;
        //        posCounter++;
        //    }

        //    return new JsonResult(result2);
        //}

        [FunctionName("QuerySubmissionCount")]
        public async Task<IActionResult> Count(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results/count")] HttpRequest req,
            ILogger log)
        {
            if (!string.IsNullOrWhiteSpace(req.Query["Verified"]))
            {
                bool.TryParse(req.Query["Verified"], out var verifyBoolResult);
                return new JsonResult(await _context.GetTotalSubmissions(verifyBoolResult));
            }
            return new JsonResult(await _context.GetTotalSubmissions(null));
        }

        [FunctionName("DataQuery")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results")] HttpRequest req,
            ILogger log)
        {
            var request = new Request
            {
                Count = int.Parse(req.Query["PageSize"]),
                Page = int.Parse(req.Query["Page"]),
                NameFilter = req.Query["SearchTerm"]
            };

            if (!string.IsNullOrWhiteSpace(req.Query["Verified"]))
            {
                if (bool.TryParse(req.Query["Verified"], out var verifyBoolResult))
                {
                    request.VerificationFilter = verifyBoolResult;
                }
            }

            var skip = request.Count * (request.Page);
            IEnumerable<Entrant> data = null;
            data = await _context.FetchByNameFilterOrdered(request.NameFilter, request.Count, skip, request.VerificationFilter);
            var result = new List<WebSupport.Entrant>();
            var posCounter = 1 + skip;
            foreach (var entrant in data)
            {
                if (entrant.Submission!.Show)
                {
                    var webEntrant = entrant.ConvertToWebEntrant();
                    webEntrant.Submission!.Position = posCounter;
                    result.Add(webEntrant);
                }

                posCounter++;
            }

            return new JsonResult(result);
        }
    }
}