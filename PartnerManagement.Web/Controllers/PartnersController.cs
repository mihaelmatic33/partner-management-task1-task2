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
    public IActionResult Index(int? highlightId)
    {
        var model = new PartnerListViewModel
        {
            Partners = [],
            PolicyForm = new PolicyFormModel(),
            HighlightPartnerId = highlightId
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PartnerListQuery query, CancellationToken cancellationToken)
    {
        var result = await _repository.GetPartnersPageAsync(query, cancellationToken);

        return Json(new
        {
            items = result.Items.Select(partner => new
            {
                partner.Id,
                partner.FullName,
                partner.PartnerNumber,
                partner.CroatianPin,
                PartnerType = ((PartnerType)partner.PartnerTypeId).ToString(),
                CreatedAtUtc = partner.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                partner.IsForeign,
                partner.IsActive,
                partner.Gender,
                partner.PolicyCount,
                TotalPolicyAmount = partner.TotalPolicyAmount.ToString("0.00"),
                partner.IsPriority,
                DetailsUrl = Url.Action(nameof(Details), "Partners", new { id = partner.Id }),
                EditUrl = Url.Action(nameof(Edit), "Partners", new { id = partner.Id }),
                PolicyFormUrl = Url.Action("Create", "Policies", new { partnerId = partner.Id })
            }),
            hasMore = result.HasMore
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["FormAction"] = nameof(Create);
        ViewData["PageTitle"] = "Unos novog partnera";
        ViewData["Description"] = "Forma validira podatke prije sigurnog spremanja u bazu.";
        ViewData["SubmitLabel"] = "Spremi partnera";
        return View(new PartnerFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartnerFormModel model, CancellationToken cancellationToken)
    {
        model.PartnerId = null;
        await ValidatePartnerModelAsync(model, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            SetFormViewDataForCreate();
            return View(model);
        }

        var partnerId = await _repository.CreatePartnerAsync(model, cancellationToken);
        TempData["StatusMessage"] = "Partner je uspješno spremljen.";
        return RedirectToAction(nameof(Index), new { highlightId = partnerId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var partner = await _repository.GetPartnerForEditAsync(id, cancellationToken);

        if (partner is null)
        {
            return NotFound();
        }

        SetFormViewDataForEdit();
        return View("Create", partner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PartnerFormModel model, CancellationToken cancellationToken)
    {
        model.PartnerId = id;

        if (!await _repository.PartnerExistsAsync(id, cancellationToken))
        {
            return NotFound();
        }

        await ValidatePartnerModelAsync(model, id, cancellationToken);

        if (!ModelState.IsValid)
        {
            SetFormViewDataForEdit();
            return View("Create", model);
        }

        await _repository.UpdatePartnerAsync(model, cancellationToken);
        TempData["StatusMessage"] = "Partner je uspješno ažuriran.";
        return RedirectToAction(nameof(Index), new { highlightId = id });
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
            partner.IsActive,
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

    private async Task ValidatePartnerModelAsync(PartnerFormModel model, int? excludePartnerId, CancellationToken cancellationToken)
    {
        if (ModelState.ContainsKey(nameof(PartnerFormModel.ExternalCode)) && !ModelState[nameof(PartnerFormModel.ExternalCode)]!.Errors.Any())
        {
            if (await _repository.ExternalCodeExistsAsync(model.ExternalCode, excludePartnerId, cancellationToken))
            {
                ModelState.AddModelError(nameof(PartnerFormModel.ExternalCode), "External code mora biti jedinstven.");
            }
        }

        if (ModelState.ContainsKey(nameof(PartnerFormModel.PartnerNumber)) && !ModelState[nameof(PartnerFormModel.PartnerNumber)]!.Errors.Any())
        {
            if (await _repository.PartnerNumberExistsAsync(model.PartnerNumber, excludePartnerId, cancellationToken))
            {
                ModelState.AddModelError(nameof(PartnerFormModel.PartnerNumber), "Partner number već postoji.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.CroatianPin) && ModelState.ContainsKey(nameof(PartnerFormModel.CroatianPin)) && !ModelState[nameof(PartnerFormModel.CroatianPin)]!.Errors.Any())
        {
            if (await _repository.CroatianPinExistsAsync(model.CroatianPin, excludePartnerId, cancellationToken))
            {
                ModelState.AddModelError(nameof(PartnerFormModel.CroatianPin), "Croatian PIN već postoji.");
            }
        }
    }

    private void SetFormViewDataForCreate()
    {
        ViewData["FormAction"] = nameof(Create);
        ViewData["PageTitle"] = "Unos novog partnera";
        ViewData["Description"] = "Forma validira podatke prije sigurnog spremanja u bazu.";
        ViewData["SubmitLabel"] = "Spremi partnera";
    }

    private void SetFormViewDataForEdit()
    {
        ViewData["FormAction"] = nameof(Edit);
        ViewData["PageTitle"] = "Uređivanje partnera";
        ViewData["Description"] = "Ažuriraj podatke partnera i status aktivnosti.";
        ViewData["SubmitLabel"] = "Spremi izmjene";
    }
}