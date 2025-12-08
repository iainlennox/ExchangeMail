using MimeKit;
using System.Text;

namespace ExchangeMail.Core.Services;

public static class WelcomeEmailGenerator
{
    public static MimeMessage Create(string username)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("ExchangeMail Team", "no-reply@exchangemail.local"));
        message.To.Add(new MailboxAddress(username, username)); // Ideally we'd have a name, but username suffices
        message.Subject = "Welcome to ExchangeMail!";

        var bodyBuilder = new BodyBuilder();

        string htmlContent = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f6f9; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #fff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #007bff; padding-bottom: 20px; }}
        .header h1 {{ margin: 0; color: #007bff; }}
        .content {{ margin-bottom: 30px; }}
        .feature-list {{ list-style-type: none; padding: 0; }}
        .feature-list li {{ margin-bottom: 15px; padding-left: 25px; position: relative; }}
        .feature-list li:before {{ content: '✓'; color: #28a745; position: absolute; left: 0; font-weight: bold; }}
        .footer {{ text-align: center; font-size: 0.9em; color: #777; border-top: 1px solid #eee; padding-top: 20px; }}
        .btn {{ display: inline-block; background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to ExchangeMail</h1>
        </div>
        <div class='content'>
            <p>Hi <strong>{username}</strong>,</p>
            <p>Welcome to your new ExchangeMail account! We're excited to have you on board.</p>
            <p>Here are just a few things you can do with your new mailbox:</p>
            <ul class='feature-list'>
                <li><strong>Send and Receive Emails</strong>: Stay connected with colleagues and friends.</li>
                <li><strong>Organize with Folders</strong>: Keep your inbox clutter-free by creating custom folders.</li>
                <li><strong>Manage Contacts</strong>: Store and manage your important contacts easily.</li>
                <li><strong>Junk Filtering</strong>: Built-in protection against unwanted spam.</li>
            </ul>
            <p>Start exploring your inbox now and make the most of your communication!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
            <p>&copy; {DateTime.Now.Year} ExchangeMail. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        bodyBuilder.HtmlBody = htmlContent;
        // Plain text fallback
        bodyBuilder.TextBody = $@"Welcome to ExchangeMail, {username}!

Welcome to your new ExchangeMail account! We're excited to have you on board.

Here are just a few things you can do with your new mailbox:
- Send and Receive Emails
- Organize with Folders
- Manage Contacts
- Junk Filtering

Start exploring your inbox now!

This is an automated message.
(c) {DateTime.Now.Year} ExchangeMail.";

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}
