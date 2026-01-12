using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Persistence.Data
{
    public class SharedDbContextFactory : IDesignTimeDbContextFactory<SharedDbContext>
    {
        public SharedDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SharedDbContext>();
            
            // Use PostgreSQL connection string - you may need to adjust this
            optionsBuilder.UseNpgsql("Host=localhost;Database=umihealth;Username=postgres;Password=password");
            
            return new SharedDbContext(optionsBuilder.Options);
        }
    }
}
