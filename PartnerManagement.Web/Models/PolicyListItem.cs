namespace PartnerManagement.Web.Models;

public sealed class PolicyListItem
{
    public int Id { get; init; }
    public string PolicyNumber { get; init; } = string.Empty;
    public decimal PolicyAmount { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}