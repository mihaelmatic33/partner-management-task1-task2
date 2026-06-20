using Dapper;
using PartnerManagement.Web.Models;

namespace PartnerManagement.Web.Data;

public sealed class PartnerRepository : IPartnerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PartnerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PartnerListItem>> GetPartnersAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.Id,
                CASE WHEN stats.PolicyCount > 5 OR stats.TotalPolicyAmount > 5000 THEN '* ' ELSE '' END + p.FirstName + ' ' + p.LastName AS FullName,
                p.PartnerNumber,
                p.CroatianPIN AS CroatianPin,
                p.PartnerTypeId,
                p.CreatedAtUtc,
                p.IsForeign,
                p.Gender,
                stats.PolicyCount,
                stats.TotalPolicyAmount,
                CASE WHEN stats.PolicyCount > 5 OR stats.TotalPolicyAmount > 5000 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPriority
            FROM dbo.Partners p
            OUTER APPLY (
                SELECT
                    COUNT(1) AS PolicyCount,
                    COALESCE(SUM(ip.PolicyAmount), 0) AS TotalPolicyAmount
                FROM dbo.InsurancePolicies ip
                WHERE ip.PartnerId = p.Id
            ) stats
            ORDER BY p.CreatedAtUtc DESC, p.Id DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<PartnerListItem>(command);
        return items.AsList();
    }

    public async Task<PartnerDetails?> GetPartnerDetailsAsync(int partnerId, CancellationToken cancellationToken)
    {
        const string detailsSql = """
            SELECT
                p.Id,
                p.FirstName + ' ' + p.LastName AS FullName,
                COALESCE(p.Address, '') AS Address,
                p.PartnerNumber,
                p.CroatianPIN AS CroatianPin,
                p.PartnerTypeId,
                p.CreatedAtUtc,
                p.CreatedByUser,
                p.IsForeign,
                p.ExternalCode,
                p.Gender
            FROM dbo.Partners p
            WHERE p.Id = @PartnerId;
            """;

        const string policiesSql = """
            SELECT
                ip.Id,
                ip.PolicyNumber,
                ip.PolicyAmount,
                ip.CreatedAtUtc
            FROM dbo.InsurancePolicies ip
            WHERE ip.PartnerId = @PartnerId
            ORDER BY ip.CreatedAtUtc DESC, ip.Id DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var partner = await connection.QuerySingleOrDefaultAsync<PartnerDetails>(new CommandDefinition(detailsSql, new { PartnerId = partnerId }, cancellationToken: cancellationToken));

        if (partner is null)
        {
            return null;
        }

        var policies = await connection.QueryAsync<PolicyListItem>(new CommandDefinition(policiesSql, new { PartnerId = partnerId }, cancellationToken: cancellationToken));
        partner.Policies = policies.AsList();
        return partner;
    }

    public async Task<int> CreatePartnerAsync(PartnerFormModel model, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Partners
            (
                FirstName,
                LastName,
                Address,
                PartnerNumber,
                CroatianPIN,
                PartnerTypeId,
                CreatedByUser,
                IsForeign,
                ExternalCode,
                Gender
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @FirstName,
                @LastName,
                @Address,
                @PartnerNumber,
                @CroatianPin,
                @PartnerTypeId,
                @CreatedByUser,
                @IsForeign,
                @ExternalCode,
                @Gender
            );
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            model.FirstName,
            model.LastName,
            Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
            model.PartnerNumber,
            CroatianPin = string.IsNullOrWhiteSpace(model.CroatianPin) ? null : model.CroatianPin,
            model.PartnerTypeId,
            model.CreatedByUser,
            model.IsForeign,
            model.ExternalCode,
            model.Gender
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExternalCodeExistsAsync(string externalCode, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE ExternalCode = @ExternalCode) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { ExternalCode = externalCode }, cancellationToken: cancellationToken));
    }

    public async Task<bool> PartnerNumberExistsAsync(string partnerNumber, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE PartnerNumber = @PartnerNumber) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { PartnerNumber = partnerNumber }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CroatianPinExistsAsync(string croatianPin, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE CroatianPIN = @CroatianPin) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { CroatianPin = croatianPin }, cancellationToken: cancellationToken));
    }

    public async Task<bool> PartnerExistsAsync(int partnerId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE Id = @PartnerId) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { PartnerId = partnerId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreatePolicyAsync(PolicyFormModel model, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.InsurancePolicies
            (
                PartnerId,
                PolicyNumber,
                PolicyAmount
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @PartnerId,
                @PolicyNumber,
                @PolicyAmount
            );
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, model, cancellationToken: cancellationToken));
    }
}