using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Capitec.Dispute.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use localdb for design-time operations
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TransactionDisputePortal;Trusted_Connection=true;");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
