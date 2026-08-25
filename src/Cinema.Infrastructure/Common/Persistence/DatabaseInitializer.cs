using System.Data;

using Dapper;

namespace Cinema.Infrastructure.Common.Persistence;

public static class DatabaseInitializer
{
    public static void Initialize(IDbConnection connection)
    {
        connection.Execute("""
            DROP TABLE IF EXISTS TestItems;
            DROP TABLE IF EXISTS Notes;

            CREATE TABLE TestItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Message TEXT NOT NULL
            );

            INSERT INTO TestItems (Message) VALUES ('test');

            CREATE TABLE Notes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL
            );
            """);
    }
}