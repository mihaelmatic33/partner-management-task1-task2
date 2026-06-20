using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PartnerManagement.Web.Models;

public sealed class PolicyFormModel : IValidatableObject
{
    private static readonly Regex PolicyPattern = new(@"^[A-Za-z0-9]+$", RegexOptions.Compiled);

    public int? PolicyId { get; set; }

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    public int PartnerId { get; set; }

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(15, MinimumLength = 10)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "Iznos police mora biti veći od 0.")]
    public decimal? PolicyAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PolicyId is <= 0)
        {
            yield return new ValidationResult("Neispravan identifikator police.", [nameof(PolicyId)]);
        }

        if (!string.IsNullOrWhiteSpace(PolicyNumber) && !PolicyPattern.IsMatch(PolicyNumber))
        {
            yield return new ValidationResult("Broj police mora biti alfanumerički bez razmaka.", [nameof(PolicyNumber)]);
        }
    }
}