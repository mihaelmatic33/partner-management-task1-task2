namespace PartnerManagement.Web.Models;

public sealed class PartnerDetails
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PartnerNumber { get; init; } = string.Empty;
    public string? CroatianPin { get; init; }
    public int PartnerTypeId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string CreatedByUser { get; init; } = string.Empty;
    public bool IsForeign { get; init; }
    public bool IsActive { get; init; }
    public string ExternalCode { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public IReadOnlyList<PolicyListItem> Policies { get; set; } = [];
}