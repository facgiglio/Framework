using System.Net;
using System.Net.Mail;
using System;

namespace Framework
{
    public class SendMail
    {
        public static void Send(Models.Mail toMail)
        {
            var fromAddress = new MailAddress(System.Configuration.ConfigurationManager.AppSettings["Address"], System.Configuration.ConfigurationManager.AppSettings["DisplayName"]);
            var fromPassword = System.Configuration.ConfigurationManager.AppSettings["Password"];

            var smtp = new SmtpClient
            {
                Host = System.Configuration.ConfigurationManager.AppSettings["Host"],
                Port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["Port"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress.Address, toMail.MailAddress.Address)
            {
                Subject = toMail.Subject,
                Body = toMail.Body
            })
            {
                smtp.Send(message);
            }


        }

    }
}

namespace Framework.Models
{
    public class Mail
    {
        public Mail(string address, string displayName, string subject, string body)
        {
            MailAddress = new MailAddress(address, displayName);
            Subject = subject;
            Body = body;
        }

        public MailAddress MailAddress { get; }
        public string Subject { get; }
        public string Body { get; }
    }
}