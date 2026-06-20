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
                p.IsActive,
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

    public async Task<PartnerListPageResult> GetPartnersPageAsync(PartnerListQuery query, CancellationToken cancellationToken)
    {
        var normalizedLimit = query.Limit <= 0 ? 10 : Math.Min(query.Limit, 100);
        var normalizedOffset = Math.Max(query.Offset, 0);

        const string sql = """
            WITH PartnerStats AS
            (
                SELECT
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    p.PartnerNumber,
                    p.CroatianPIN AS CroatianPin,
                    p.PartnerTypeId,
                    p.CreatedAtUtc,
                    p.IsForeign,
                    p.IsActive,
                    p.Gender,
                    stats.PolicyCount,
                    stats.TotalPolicyAmount,
                    CASE WHEN stats.PolicyCount > 5 OR stats.TotalPolicyAmount > 5000 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPriority
                FROM dbo.Partners p
                OUTER APPLY
                (
                    SELECT
                        COUNT(1) AS PolicyCount,
                        COALESCE(SUM(ip.PolicyAmount), 0) AS TotalPolicyAmount
                    FROM dbo.InsurancePolicies ip
                    WHERE ip.PartnerId = p.Id
                ) stats
            )
            SELECT
                ps.Id,
                CASE WHEN ps.IsPriority = 1 THEN '* ' ELSE '' END + ps.FirstName + ' ' + ps.LastName AS FullName,
                ps.PartnerNumber,
                ps.CroatianPin,
                ps.PartnerTypeId,
                ps.CreatedAtUtc,
                ps.IsForeign,
                ps.IsActive,
                ps.Gender,
                ps.PolicyCount,
                ps.TotalPolicyAmount,
                ps.IsPriority
            FROM PartnerStats ps
            WHERE
                (@Name IS NULL OR (ps.FirstName + ' ' + ps.LastName) LIKE '%' + @Name + '%')
                AND (@CroatianPin IS NULL OR ISNULL(ps.CroatianPin, '') LIKE '%' + @CroatianPin + '%')
                AND (@CreatedFrom IS NULL OR CAST(ps.CreatedAtUtc AS date) >= @CreatedFrom)
                AND (@CreatedTo IS NULL OR CAST(ps.CreatedAtUtc AS date) <= @CreatedTo)
                AND (@MinPolicyAmount IS NULL OR ps.TotalPolicyAmount >= @MinPolicyAmount)
                AND (@MaxPolicyAmount IS NULL OR ps.TotalPolicyAmount <= @MaxPolicyAmount)
                AND
                (
                    @Search IS NULL
                    OR (ps.FirstName + ' ' + ps.LastName) LIKE '%' + @Search + '%'
                    OR ps.PartnerNumber LIKE '%' + @Search + '%'
                    OR ISNULL(ps.CroatianPin, '') LIKE '%' + @Search + '%'
                    OR CONVERT(varchar(64), ps.TotalPolicyAmount) LIKE '%' + @Search + '%'
                )
            ORDER BY ps.CreatedAtUtc DESC, ps.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @TakePlusOne ROWS ONLY;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<PartnerListItem>(new CommandDefinition(
            sql,
            new
            {
                Offset = normalizedOffset,
                TakePlusOne = normalizedLimit + 1,
                Search = NormalizeText(query.Search),
                Name = NormalizeText(query.Name),
                CroatianPin = NormalizeText(query.CroatianPin),
                query.CreatedFrom,
                query.CreatedTo,
                query.MinPolicyAmount,
                query.MaxPolicyAmount
            },
            cancellationToken: cancellationToken))).AsList();

        var hasMore = rows.Count > normalizedLimit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new PartnerListPageResult
        {
            Items = rows,
            HasMore = hasMore
        };
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
                p.IsActive,
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

    public async Task<PartnerFormModel?> GetPartnerForEditAsync(int partnerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.Id AS PartnerId,
                p.FirstName,
                p.LastName,
                p.Address,
                p.PartnerNumber,
                p.CroatianPIN AS CroatianPin,
                p.PartnerTypeId,
                p.CreatedByUser,
                p.IsForeign,
                p.IsActive,
                p.ExternalCode,
                p.Gender
            FROM dbo.Partners p
            WHERE p.Id = @PartnerId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PartnerFormModel>(
            new CommandDefinition(sql, new { PartnerId = partnerId }, cancellationToken: cancellationToken));
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
                IsActive,
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
                @IsActive,
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
            PartnerTypeId = model.PartnerTypeId!.Value,
            model.CreatedByUser,
            model.IsForeign,
            model.IsActive,
            model.ExternalCode,
            model.Gender
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdatePartnerAsync(PartnerFormModel model, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Partners
            SET
                FirstName = @FirstName,
                LastName = @LastName,
                Address = @Address,
                PartnerNumber = @PartnerNumber,
                CroatianPIN = @CroatianPin,
                PartnerTypeId = @PartnerTypeId,
                CreatedByUser = @CreatedByUser,
                IsForeign = @IsForeign,
                IsActive = @IsActive,
                ExternalCode = @ExternalCode,
                Gender = @Gender
            WHERE Id = @PartnerId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            model.PartnerId,
            model.FirstName,
            model.LastName,
            Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
            model.PartnerNumber,
            CroatianPin = string.IsNullOrWhiteSpace(model.CroatianPin) ? null : model.CroatianPin,
            PartnerTypeId = model.PartnerTypeId!.Value,
            model.CreatedByUser,
            model.IsForeign,
            model.IsActive,
            model.ExternalCode,
            model.Gender
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExternalCodeExistsAsync(string externalCode, int? excludePartnerId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE ExternalCode = @ExternalCode AND (@ExcludePartnerId IS NULL OR Id <> @ExcludePartnerId)) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { ExternalCode = externalCode, ExcludePartnerId = excludePartnerId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> PartnerNumberExistsAsync(string partnerNumber, int? excludePartnerId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE PartnerNumber = @PartnerNumber AND (@ExcludePartnerId IS NULL OR Id <> @ExcludePartnerId)) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { PartnerNumber = partnerNumber, ExcludePartnerId = excludePartnerId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> CroatianPinExistsAsync(string croatianPin, int? excludePartnerId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Partners WHERE CroatianPIN = @CroatianPin AND (@ExcludePartnerId IS NULL OR Id <> @ExcludePartnerId)) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { CroatianPin = croatianPin, ExcludePartnerId = excludePartnerId }, cancellationToken: cancellationToken));
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
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            model.PartnerId,
            model.PolicyNumber,
            PolicyAmount = model.PolicyAmount!.Value
        }, cancellationToken: cancellationToken));
    }

    public async Task<PolicyFormModel?> GetPolicyForEditAsync(int policyId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                ip.Id AS PolicyId,
                ip.PartnerId,
                ip.PolicyNumber,
                ip.PolicyAmount
            FROM dbo.InsurancePolicies ip
            WHERE ip.Id = @PolicyId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PolicyFormModel>(
            new CommandDefinition(sql, new { PolicyId = policyId }, cancellationToken: cancellationToken));
    }

    public async Task UpdatePolicyAsync(PolicyFormModel model, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.InsurancePolicies
            SET
                PolicyNumber = @PolicyNumber,
                PolicyAmount = @PolicyAmount
            WHERE Id = @PolicyId
                AND PartnerId = @PartnerId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            model.PolicyId,
            model.PartnerId,
            model.PolicyNumber,
            PolicyAmount = model.PolicyAmount!.Value
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> PolicyExistsAsync(int policyId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.InsurancePolicies WHERE Id = @PolicyId) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { PolicyId = policyId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> PolicyNumberExistsAsync(string policyNumber, int? excludePolicyId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.InsurancePolicies WHERE PolicyNumber = @PolicyNumber AND (@ExcludePolicyId IS NULL OR Id <> @ExcludePolicyId)) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { PolicyNumber = policyNumber, ExcludePolicyId = excludePolicyId }, cancellationToken: cancellationToken));
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}