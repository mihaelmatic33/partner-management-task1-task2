namespace PartnerManagement.Web.Models;

public sealed class PartnerListItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PartnerNumber { get; init; } = string.Empty;
    public string? CroatianPin { get; init; }
    public int PartnerTypeId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public bool IsForeign { get; init; }
    public string Gender { get; init; } = string.Empty;
    public int PolicyCount { get; init; }
    public decimal TotalPolicyAmount { get; init; }
    public bool IsPriority { get; init; }
}