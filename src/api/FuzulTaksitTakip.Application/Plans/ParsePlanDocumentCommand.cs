using FluentValidation;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using MediatR;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record ParsePlanDocumentCommand(
    Guid PlanId,
    Stream Content,
    string FileName,
    string ContentType) : IRequest<PlanDocumentPreviewDto>;

public sealed record PlanDocumentPreviewDto(
    string SourceFileName,
    string SourceKind,
    string Title,
    string Description,
    decimal GrandTotal,
    int InstallmentCount,
    string? DeliveryName,
    int DeliveryIndex,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<TemplatePartnerPreviewDto> Partners,
    IReadOnlyList<TemplateInstallmentPreviewDto> Installments);

public sealed class ParsePlanDocumentCommandHandler : IRequestHandler<ParsePlanDocumentCommand, PlanDocumentPreviewDto>
{
    private readonly IPlanAuthorization _auth;
    private readonly IPlanDocumentParser _parser;

    public ParsePlanDocumentCommandHandler(IPlanAuthorization auth, IPlanDocumentParser parser)
    {
        _auth = auth;
        _parser = parser;
    }

    public async Task<PlanDocumentPreviewDto> Handle(ParsePlanDocumentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        ParsedPlanDocument parsed;
        try
        {
            parsed = await _parser.ParseAsync(
                request.Content,
                request.FileName,
                request.ContentType,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        var partnerCount = Math.Max(parsed.Partners.Count, 1);
        var installments = parsed.Installments
            .Select(i => new TemplateInstallmentPreviewDto(
                i.Index,
                i.Name,
                i.DueDate,
                i.TotalAmount,
                Math.Round(i.TotalAmount / partnerCount, 2)))
            .ToList();

        var partners = parsed.Partners
            .Select(p => new TemplatePartnerPreviewDto(p.Name, p.Color, p.DefaultPct))
            .ToList();

        var deliveryName = parsed.DeliveryIndex >= 0 && parsed.DeliveryIndex < installments.Count
            ? installments[parsed.DeliveryIndex].Name
            : null;

        return new PlanDocumentPreviewDto(
            parsed.SourceFileName,
            parsed.SourceKind,
            parsed.Title,
            parsed.Description,
            installments.Sum(i => i.TotalAmount),
            installments.Count,
            deliveryName,
            parsed.DeliveryIndex,
            parsed.Warnings,
            partners,
            installments);
    }
}
