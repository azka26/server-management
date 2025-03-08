using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServerManagementApi.Models.Entities.Configurations
{
    public class DeployPackageConfiguration : IEntityTypeConfiguration<DeployPackage>
    {
        public void Configure(EntityTypeBuilder<DeployPackage> builder)
        {
            builder.Property(f => f.Id).ValueGeneratedOnAdd();
            builder.HasKey(f => f.Id);
        }
    }
}
