namespace FuzulTaksitTakip.Infrastructure.Email;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";

    /// <summary>Local hour in Europe/Istanbul when the daily reminder job should run (0-23).</summary>
    public int RunHourLocal { get; set; } = 9;

    /// <summary>When true, also run once shortly after API startup (useful for local smoke tests).</summary>
    public bool RunOnStartup { get; set; }
}
