using System.Data.Entity;

namespace AngelsChat.Server.Data
{
    public class PostgresqlConfiguration : DbConfiguration
    {
        public PostgresqlConfiguration()
        {
            SetProviderServices("Npgsql", Npgsql.NpgsqlServices.Instance);
            SetProviderFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            SetDefaultConnectionFactory(new Npgsql.NpgsqlConnectionFactory());
        }
    }
}
