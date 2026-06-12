using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WebBanHoa.Models
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var fromMail = "your-email@gmail.com";
            var fromPassword = "your-app-password"; // Mã bảo mật ứng dụng Google App Password

            var message = new MailMessage();
            message.From = new MailAddress(fromMail, "Floral LAM - Luxury Boutique");
            message.Subject = subject;
            message.To.Add(new MailAddress(email));
            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential(fromMail, fromPassword);
                smtpClient.EnableSsl = true;
                try
                {
                    smtpClient.Send(message); // Bỏ comment dòng này nếu cấu hình mail thật
                }
                catch { }
            }

            return Task.CompletedTask;
        }
    }
}