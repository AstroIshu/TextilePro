using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TextilePro.Core.DbContext;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Use the same connection string as your app
        optionsBuilder.UseSqlite("Data Source=TextilePro.db");
        
        return new AppDbContext(optionsBuilder.Options);
    }
}