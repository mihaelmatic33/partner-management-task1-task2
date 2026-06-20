using Microsoft.AspNetCore.Mvc;
using PartnerManagement.Web.Data;
using PartnerManagement.Web.Models;

namespace PartnerManagement.Web.Controllers;

public sealed class PartnersController : Controller
{
    private readonly IPartnerRepository _repository;

    public PartnersController(IPartnerRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? highlightId, CancellationToken cancellationToken)
    {
        var model = new PartnerListViewModel
        {
            Partners = await _repository.GetPartnersAsync(cancellationToken),
            PolicyForm = new PolicyFormModel(),
            HighlightPartnerId = highlightId
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PartnerFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartnerFormModel model, CancellationToken cancellationToken)
    {
        await ValidatePartnerModelAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var partnerId = await _repository.CreatePartnerAsync(model, cancellationToken);
        TempData["StatusMessage"] = "Partner je uspješno spremljen.";
        return RedirectToAction(nameof(Index), new { highlightId = partnerId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var partner = await _repository.GetPartnerDetailsAsync(id, cancellationToken);

        if (partner is null)
        {
            return NotFound();
        }

        return Json(new
        {
            partner.Id,
            partner.FullName,
            partner.Address,
            partner.PartnerNumber,
            partner.CroatianPin,
            PartnerType = ((PartnerType)partner.PartnerTypeId).ToString(),
            CreatedAtUtc = partner.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
            partner.CreatedByUser,
            partner.IsForeign,
            partner.ExternalCode,
            partner.Gender,
            Policies = partner.Policies.Select(policy => new
            {
                policy.Id,
                policy.PolicyNumber,
                PolicyAmount = policy.PolicyAmount.ToString("0.00"),
                CreatedAtUtc = policy.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
            })
        });
    }

    private async Task ValidatePartnerModelAsync(PartnerFormModel model, CancellationToken cancellationToken)
    {
        if (ModelState.ContainsKey(nameof(PartnerFormModel.ExternalCode)) && !ModelState[nameof(PartnerFormModel.ExternalCode)]!.Errors.Any())
        {
            if (await _repository.ExternalCodeExistsAsync(model.ExternalCode, cancellationToken))
            {
                ModelState.AddModelError(nameof(PartnerFormModel.ExternalCode), "External code mora biti jedinstven.");
            }
        }

        if (ModelState.ContainsKey(nameof(PartnerFormModel.PartnerNumber)) && !ModelState[nameof(PartnerFormModel.PartnerNumber)]!.Errors.Any())
        {
            if (await _repository.PartnerNumberExistsAsync(model.PartnerNumber, cancellationToken))
            {
                ModelState.AddModelError(nameof(PartnerFormModel.PartnerNumber), "Partner number već postoji.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.CroatianPin) && ModelState.ContainsKey(nameof(PartnerFormModel.CroatianPin)) && !ModelState[nameof(PartnerFormModel.CroatianPin)]!.Errors.Any())
        {
            if (await _repository.CroatianPinExistsAsync(model.CroatianPin, cancellationToken))
            {
                ModelState.AddModelError(nameof(PartnerFormModel.CroatianPin), "Croatian PIN već postoji.");
            }
        }
    }
}