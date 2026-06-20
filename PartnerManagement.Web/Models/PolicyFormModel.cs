using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PartnerManagement.Web.Models;

public sealed class PolicyFormModel : IValidatableObject
{
    private static readonly Regex PolicyPattern = new(@"^[A-Za-z0-9]+$", RegexOptions.Compiled);

    [Required]
    public int PartnerId { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 10)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal PolicyAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PolicyPattern.IsMatch(PolicyNumber))
        {
            yield return new ValidationResult("Broj police mora biti alfanumerički bez razmaka.", [nameof(PolicyNumber)]);
        }
    }
}