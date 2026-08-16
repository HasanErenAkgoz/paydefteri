namespace PayDefteri.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>When false, emails are logged only (local/dev friendly).</summary>
    public bool Enabled { get; set; }

    public string FromAddress { get; set; } = "noreply@paydefteri.local";
    public string FromName { get; set; } = "PayDefteri";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public Angular/web origin used in invite links, e.g. https://app.paydefteri.com</summary>
    public string PublicWebUrl { get; set; } = "http://localhost:4200";
}
