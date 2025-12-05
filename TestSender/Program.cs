using System;
using MimeKit;
using MailKit.Net.Smtp;

namespace TestSender
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Test Sender", "sender@example.com"));
                message.To.Add(new MailboxAddress("Test Recipient", "iain@lab.lennoxfamily.net"));
                message.Subject = "Test Email from Console";

                message.Body = new TextPart("plain")
                {
                    Text = @"This is a test email sent from a console app to verify the fix."
                };

                using (var client = new SmtpClient())
                {
                    // Accept all SSL certificates (in case of self-signed)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    Console.WriteLine("Connecting to localhost:2525...");
                    client.Connect("localhost", 2525, false);

                    Console.WriteLine("Sending message...");
                    client.Send(message);

                    Console.WriteLine("Message sent successfully!");
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
