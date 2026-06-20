namespace PartnerManagement.Web.Models;

public sealed class PartnerListViewModel
{
    public IReadOnlyList<PartnerListItem> Partners { get; init; } = [];
    public PolicyFormModel PolicyForm { get; init; } = new();
    public int? HighlightPartnerId { get; init; }
}