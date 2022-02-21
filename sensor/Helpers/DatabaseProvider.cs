using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using sensor.Dtos;
using System.Data.Common;

namespace sensor.Helpers;

public class DatabaseProvider : IDatabaseProvider
{
    private readonly IOptions<ConnectionStrings> _connectionStrings;

    public DatabaseProvider(IOptions<ConnectionStrings> connectionStrings)
    {
        _connectionStrings = connectionStrings;
    }
    public DbConnection GetConnection()
    {
        return new SqliteConnection(_connectionStrings.Value.Sqlite);
    }
}
