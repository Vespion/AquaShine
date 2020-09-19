using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AquaShine.WebSupport.Api.Submission;
using MatBlazor;

namespace AquaShine.WebFacade.Helpers
{
    public class ResultsSubmissionModel
    {
        public InitalRequest? InitialRequest { get; set; }
        public InitalResponse? InitialResponse { get; set; }
        public long? EntrantId { get; set; }
        public IMatFileUploadEntry? VerificationFileEntry { get; set; }
        public IMatFileUploadEntry? DisplayFileEntry { get; set; }

        //TODO Display
    }
}
