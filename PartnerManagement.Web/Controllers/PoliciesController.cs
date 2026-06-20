using Microsoft.AspNetCore.Mvc;
using PartnerManagement.Web.Data;
using PartnerManagement.Web.Models;

namespace PartnerManagement.Web.Controllers;

public sealed class PoliciesController : Controller
{
    private readonly IPartnerRepository _repository;

    public PoliciesController(IPartnerRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int partnerId, CancellationToken cancellationToken)
    {
        if (!await _repository.PartnerExistsAsync(partnerId, cancellationToken))
        {
            return NotFound();
        }

        return View(new PolicyFormModel { PartnerId = partnerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PolicyFormModel model, CancellationToken cancellationToken)
    {
        await ValidatePolicyModelAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json(new
                {
                    success = false,
                    errors = ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray())
                });
            }

            return View(model);
        }

        await _repository.CreatePolicyAsync(model, cancellationToken);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            var partners = await _repository.GetPartnersAsync(cancellationToken);
            var updatedPartner = partners.Single(item => item.Id == model.PartnerId);

            return Json(new
            {
                success = true,
                partnerId = updatedPartner.Id,
                fullName = updatedPartner.FullName,
                policyCount = updatedPartner.PolicyCount,
                totalPolicyAmount = updatedPartner.TotalPolicyAmount.ToString("0.00"),
                isPriority = updatedPartner.IsPriority
            });
        }

        TempData["StatusMessage"] = "Polica je uspješno spremljena.";
        return RedirectToAction("Index", "Partners", new { highlightId = model.PartnerId });
    }

    private async Task ValidatePolicyModelAsync(PolicyFormModel model, CancellationToken cancellationToken)
    {
        if (!await _repository.PartnerExistsAsync(model.PartnerId, cancellationToken))
        {
            ModelState.AddModelError(nameof(PolicyFormModel.PartnerId), "Odabrani partner ne postoji.");
        }
    }
}