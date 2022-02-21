using Dapper;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace sensor.Helpers;

public class DatabaseBootstrap : IDatabaseBootstrap
{
    private readonly IDatabaseProvider _databaseProvider;

    public DatabaseBootstrap(IDatabaseProvider databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    public void Setup()
    {
        using var connection = _databaseProvider.GetConnection();
        CreateTemperatureTable(connection);
        CreateThresholdsTable(connection);
    }

    private static void CreateTemperatureTable(DbConnection connection)
    {
        var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Temperatures';");
        var tableName = table.FirstOrDefault();
        if (!string.IsNullOrEmpty(tableName) && tableName == "Temperatures")
            return;

        connection.Execute("Create Table Temperatures (" +
            "Id INTEGER PRIMARY KEY AUTOINCREMENT," + 
            "Temperature REAL NOT NULL);");

    }

    private static void CreateThresholdsTable(DbConnection connection)
    {
        const string ThresholdsTable = "Thresholds";
        var table = connection.Query<string>($"SELECT name FROM sqlite_master WHERE type='table' AND name = '{ThresholdsTable}';");
        var tableName = table.FirstOrDefault();
        if (!string.IsNullOrEmpty(tableName) && tableName == ThresholdsTable)
            return;

        connection.Execute($"Create Table {ThresholdsTable} (" +
            "Cold INTEGRER NOT NULL," +
            "Hot INTEGER NOT NULL);");

        connection.Execute($"INSERT INTO {ThresholdsTable} (" +
            "Cold, Hot) VALUES (20, 42);");
    }
}
