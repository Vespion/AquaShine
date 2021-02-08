using AquaShine.WebSupport;
using System;
using Entrant = AquaShine.ApiHub.Data.Models.Entrant;

namespace AquaShine.ApiFacade.Helpers
{
    public static class Extensions
    {
        public static AquaShine.WebSupport.Entrant ConvertToWebEntrant(this Entrant entrant)
        {
            return new WebSupport.Entrant
            {
                BioGender = Enum.Parse<WebSupport.Gender>(entrant.BioGender.ToString()),
                Id = entrant.RowKey,
                Name = entrant.Submission?.DisplayName ?? entrant.Name ?? "(unnamed)",
                Submission = new Submission
                {
                    DisplayImgUrl = entrant.Submission?.DisplayImgUrl,
                    VerifyingImgUrl = entrant.Submission?.VerifyingImgUrl,
                    Verified = entrant.Submission?.Verified ?? false,
                    TimeToComplete = entrant.Submission?.TimeToComplete
                }
            };
        }
    }
}