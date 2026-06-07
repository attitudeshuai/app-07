using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PointsMall.Data;

namespace PointsMall;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            var server = configuration["DB_SERVER"] ?? "localhost";
            var database = configuration["DB_DATABASE"] ?? "PointsMallDB";
            var user = configuration["DB_USER"] ?? "root";
            var password = configuration["DB_PASSWORD"] ?? string.Empty;
            connectionString = $"Server={server};Database={database};User={user};Password={password};";
        }

        var serverVersionStr = configuration["MySqlServerVersion"];
        ServerVersion serverVersion;
        if (!string.IsNullOrEmpty(serverVersionStr))
        {
            serverVersion = ServerVersion.Parse(serverVersionStr);
        }
        else
        {
            serverVersion = ServerVersion.Parse("8.0.33-mysql");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
