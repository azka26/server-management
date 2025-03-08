using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ServerManagementApi.Models.Entities;
using ServerManagementApi.Models.Entities.Configurations;

namespace ServerManagementApi
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PackageDeployment> PackageDeployment { get; set; }
        public DbSet<DeployPackage> DeployPackage { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration<DeployPackage>(new DeployPackageConfiguration());
            modelBuilder.ApplyConfiguration<PackageDeployment>(new PackageDeploymentConfiguration());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
