using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AquaShine.Emails.Templates;
using MimeKit;

namespace AquaShine.Emails.Client
{
    public interface IMailClient
    {
        Task SendMessage<TMessage>(TMessage message, MailboxAddress sender, MailboxAddress target) where TMessage : IEmailMessage;
    }
}
