namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogGenderdEntityTypeConfiguration
    : IEntityTypeConfiguration<CatalogGender>
{
    public void Configure(EntityTypeBuilder<CatalogGender> builder)
    {
        builder.ToTable("CatalogGender");

        builder.Property(cb => cb.Brand)
            .HasMaxLength(100);
    }
}
