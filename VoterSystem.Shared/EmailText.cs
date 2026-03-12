namespace VoterSystem.Shared;

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

    public static string GetTwoFactorEnabledEmail(string to)
    {
        return $"""
                <html>
                    <body style="font-family: sans-serif; line-height: 1.5;">
                        <h1>Dear {to}!</h1>
                        <p>Two-factor authentication has been enabled on your account.</p>
                        <p>If you did not make this change, reset your password and review your account access immediately.</p>
                    </body>
                </html>
                """;
    }

    public static string GetTwoFactorCodeEmail(string to, string code)
    {
        return $"""
                <html>
                    <body style="font-family: sans-serif; line-height: 1.5;">
                        <h1>Dear {to}!</h1>
                        <p>Your two-factor authentication code is:</p>
                        <p style="font-size: 2rem; font-weight: bold; letter-spacing: 0.3rem;">{code}</p>
                        <p>This code expires in 5 minutes.</p>
                    </body>
                </html>
                """;
    }
}
