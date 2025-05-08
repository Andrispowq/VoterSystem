namespace VoterSystem.WebAPI.Config;

public static class EmailText
{
    public static string GetEmail(string to, string type, string link)
    {
        return $"""
                <html>
                    <head>
                        <meta charset="UTF-8">
                        <title>Password reset code</title>
                    </head>
                    <body style="font-family: sans-serif; line-height: 1.5;">
                        <h1>Dear {to}!</h1>
                        <p>Your {type} link is:
                            <a href="{link}" target="_blank" style="color: #1a73e8;">Click here</a>.
                        </p>
                        <p>If the link doesn't work, copy and paste this into your browser:</p>
                        <p style="word-break: break-all;">{link}</p>
                    </body>
                </html>
                """;
    }
}