using MailChemist.Core.Attributes;
using MailChemist.Providers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AquaShine.Emails.Templates
{
    public class EntrantCreated : IEmailMessage
    {
        private readonly string _compliedPath;

        public EntrantCreated(string compliedPath)
        {
            _compliedPath = compliedPath;
        }

        [MailChemistModel]
        public class Model
        {
            public long EntrantNum { get; set; }
            public string FirstName { get; set; }
        }

        public Model EmailVariables { get; } = new Model();

        public string Subject { get; set; }

        public Task<string> Generate()
        {
            return Task.Run(() =>
            {
                var mailChemist = new MailChemist.MailChemist(new FileEmailContentProvider(_compliedPath));
                if (!mailChemist.TryGenerate("EntrantCreated.mc", EmailVariables, out var result, out var errors))
                {
                    throw new AggregateException(errors.Select(e => new Exception(e)));
                }

                return result;
            });
        }
    }

    public interface IEmailMessage
    {
        string Subject { get; set; }

        Task<string> Generate();
    }
}