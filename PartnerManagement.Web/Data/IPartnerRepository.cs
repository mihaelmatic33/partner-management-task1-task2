using PartnerManagement.Web.Models;

namespace PartnerManagement.Web.Data;

public interface IPartnerRepository
{
    Task<IReadOnlyList<PartnerListItem>> GetPartnersAsync(CancellationToken cancellationToken);
    Task<PartnerDetails?> GetPartnerDetailsAsync(int partnerId, CancellationToken cancellationToken);
    Task<int> CreatePartnerAsync(PartnerFormModel model, CancellationToken cancellationToken);
    Task<bool> ExternalCodeExistsAsync(string externalCode, CancellationToken cancellationToken);
    Task<bool> PartnerNumberExistsAsync(string partnerNumber, CancellationToken cancellationToken);
    Task<bool> CroatianPinExistsAsync(string croatianPin, CancellationToken cancellationToken);
    Task<bool> PartnerExistsAsync(int partnerId, CancellationToken cancellationToken);
    Task<int> CreatePolicyAsync(PolicyFormModel model, CancellationToken cancellationToken);
}