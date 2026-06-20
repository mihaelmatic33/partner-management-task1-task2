using PartnerManagement.Web.Models;

namespace PartnerManagement.Web.Data;

public interface IPartnerRepository
{
    Task<IReadOnlyList<PartnerListItem>> GetPartnersAsync(CancellationToken cancellationToken);
    Task<PartnerListPageResult> GetPartnersPageAsync(PartnerListQuery query, CancellationToken cancellationToken);
    Task<PartnerDetails?> GetPartnerDetailsAsync(int partnerId, CancellationToken cancellationToken);
    Task<PartnerFormModel?> GetPartnerForEditAsync(int partnerId, CancellationToken cancellationToken);
    Task<int> CreatePartnerAsync(PartnerFormModel model, CancellationToken cancellationToken);
    Task UpdatePartnerAsync(PartnerFormModel model, CancellationToken cancellationToken);
    Task<bool> ExternalCodeExistsAsync(string externalCode, int? excludePartnerId, CancellationToken cancellationToken);
    Task<bool> PartnerNumberExistsAsync(string partnerNumber, int? excludePartnerId, CancellationToken cancellationToken);
    Task<bool> CroatianPinExistsAsync(string croatianPin, int? excludePartnerId, CancellationToken cancellationToken);
    Task<bool> PartnerExistsAsync(int partnerId, CancellationToken cancellationToken);
    Task<int> CreatePolicyAsync(PolicyFormModel model, CancellationToken cancellationToken);
    Task<PolicyFormModel?> GetPolicyForEditAsync(int policyId, CancellationToken cancellationToken);
    Task UpdatePolicyAsync(PolicyFormModel model, CancellationToken cancellationToken);
    Task<bool> PolicyExistsAsync(int policyId, CancellationToken cancellationToken);
    Task<bool> PolicyNumberExistsAsync(string policyNumber, int? excludePolicyId, CancellationToken cancellationToken);
}