using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PartnerManagement.Web.Models;

public sealed class PartnerFormModel : IValidatableObject
{
    private static readonly Regex AlphanumericPattern = new(@"^[\p{L}\p{N} .,'\-/]+$", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DigitsPattern = new(@"^\d+$", RegexOptions.Compiled);
    private static readonly Regex ExternalCodePattern = new(@"^[A-Za-z0-9]+$", RegexOptions.Compiled);

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(255, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(255, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(20, MinimumLength = 20)]
    public string PartnerNumber { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11)]
    public string? CroatianPin { get; set; }

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [Range(1, 2, ErrorMessage = "Ovo polje je obavezno.")]
    public int? PartnerTypeId { get; set; }

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(255)]
    public string CreatedByUser { get; set; } = string.Empty;

    public int? PartnerId { get; set; }

    public bool IsForeign { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(20, MinimumLength = 10)]
    public string ExternalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ovo polje je obavezno.")]
    [StringLength(1, MinimumLength = 1)]
    public string Gender { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsAlphanumericText(FirstName))
        {
            yield return new ValidationResult("First name mora biti alfanumerički.", [nameof(FirstName)]);
        }

        if (!IsAlphanumericText(LastName))
        {
            yield return new ValidationResult("Last name mora biti alfanumerički.", [nameof(LastName)]);
        }

        if (!string.IsNullOrWhiteSpace(Address) && !IsAlphanumericText(Address))
        {
            yield return new ValidationResult("Address mora biti alfanumerički.", [nameof(Address)]);
        }

        if (PartnerNumber.Length != 20 || !DigitsPattern.IsMatch(PartnerNumber))
        {
            yield return new ValidationResult("Partner number mora sadržavati točno 20 znamenki.", [nameof(PartnerNumber)]);
        }

        if (!string.IsNullOrWhiteSpace(CroatianPin) && !IsValidOib(CroatianPin))
        {
            yield return new ValidationResult("Croatian PIN mora biti ispravan OIB.", [nameof(CroatianPin)]);
        }

        if (!EmailPattern.IsMatch(CreatedByUser))
        {
            yield return new ValidationResult("Created by user mora biti ispravna email adresa.", [nameof(CreatedByUser)]);
        }

        if (!ExternalCodePattern.IsMatch(ExternalCode))
        {
            yield return new ValidationResult("External code mora biti alfanumerički bez razmaka.", [nameof(ExternalCode)]);
        }

        if (Gender is not ("M" or "F" or "N"))
        {
            yield return new ValidationResult("Gender može biti samo M, F ili N.", [nameof(Gender)]);
        }
    }

    private static bool IsAlphanumericText(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && AlphanumericPattern.IsMatch(value);
    }

    private static bool IsValidOib(string value)
    {
        if (value.Length != 11 || !DigitsPattern.IsMatch(value))
        {
            return false;
        }

        var control = 10;

        for (var i = 0; i < 10; i++)
        {
            control += value[i] - '0';
            control %= 10;

            if (control == 0)
            {
                control = 10;
            }

            control *= 2;
            control %= 11;
        }

        var expected = 11 - control;

        if (expected == 10)
        {
            expected = 0;
        }

        return expected == value[10] - '0';
    }
}