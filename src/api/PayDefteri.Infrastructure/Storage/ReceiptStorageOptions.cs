namespace PayDefteri.Infrastructure.Storage;

public sealed class ReceiptStorageOptions
{
    public const string SectionName = "Storage";

    public string ReceiptsPath { get; set; } = "App_Data/receipts";
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}
