using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PointsMall.Data;

namespace PointsMall;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 33));
        optionsBuilder.UseMySql("Server=localhost;Database=PointsMallDB;User=root;Password=root;", serverVersion);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
