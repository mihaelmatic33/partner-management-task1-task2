namespace PartnerManagement.Web.Models;

public sealed class PartnerListQuery
{
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
    public string? Name { get; set; }
    public string? CroatianPin { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public decimal? MinPolicyAmount { get; set; }
    public decimal? MaxPolicyAmount { get; set; }
    public bool? OnlyActive { get; set; }
}