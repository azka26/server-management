using Microsoft.EntityFrameworkCore;
using ServerManagementApi.Models;

namespace ServerManagementApi
{
    public class AppDbContext : DbContext
    {
        public DbSet<PackageDeployment> PackageDeployment { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var packageDeployment = modelBuilder.Entity<PackageDeployment>();
            packageDeployment.HasKey(f => f.Id);
            packageDeployment.Property(f => f.Id).ValueGeneratedOnAdd();
        }

        public override int SaveChanges()
        {
            foreach (var entity in ChangeTracker.Entries())
            {
                if (entity.State == EntityState.Added)
                {
                    if (entity.Entity is BaseModel baseModel)
                    {
                        baseModel.CreatedDate = DateTime.Now;
                    }
                }
            }

            return base.SaveChanges();
        }
    }
}
