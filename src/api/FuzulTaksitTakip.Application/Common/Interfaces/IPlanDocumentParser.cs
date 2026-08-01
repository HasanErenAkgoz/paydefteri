namespace FuzulTaksitTakip.Application.Common.Interfaces;

public sealed record ParsedPlanDocument(
    string Title,
    string Description,
    string SourceFileName,
    string SourceKind,
    int DeliveryIndex,
    IReadOnlyList<ParsedPartner> Partners,
    IReadOnlyList<ParsedInstallment> Installments,
    IReadOnlyList<string> Warnings);

public sealed record ParsedPartner(string Name, string Color, decimal DefaultPct);

public sealed record ParsedInstallment(
    int Index,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount);

public interface IPlanDocumentParser
{
    Task<ParsedPlanDocument> ParseAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
