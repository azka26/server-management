using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServerManagementApi.Models.Entities.Configurations
{
    public class PackageDeploymentConfiguration : IEntityTypeConfiguration<PackageDeployment>
    {
        public void Configure(EntityTypeBuilder<PackageDeployment> builder)
        {
            builder.Property(f => f.Id).ValueGeneratedOnAdd();
            builder.HasKey(f => f.Id);
        }
    }
}
