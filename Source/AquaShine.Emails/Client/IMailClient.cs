using AquaShine.Emails.Templates;
using MimeKit;
using System.Threading.Tasks;

namespace AquaShine.Emails.Client
{
    public interface IMailClient
    {
        Task SendMessage<TMessage>(TMessage message, MailboxAddress sender, MailboxAddress target) where TMessage : IEmailMessage;
    }
}