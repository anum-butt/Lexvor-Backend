using System.Threading.Tasks;

namespace Lexvor.Services
{
    public interface IEmailSender {
	    Task SendEmailAsync(string email, string subject, string plainMessage, string htmlMessage = "", string replyTo = "");
	    Task SendEmailAsync(string[] emails, string subject, string plainMessage, string htmlMessage = "", string replyTo = "");
		Task SendEmailAsync(string emails, string subject, string plainMessage,string attachmentURL,bool isAttachment, byte[] blob, string htmlMessage = "", string replyTo = "");

	}
}
