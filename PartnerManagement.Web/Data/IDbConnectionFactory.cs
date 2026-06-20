using System.Data;

namespace PartnerManagement.Web.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}