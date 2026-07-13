namespace UPS.ReLoop.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPS.ReLoop.Domain.Entities;

public class ReturnConfiguration : IEntityTypeConfiguration<Return>
{
    public void Configure(EntityTypeBuilder<Return> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ProductId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ReturnReason).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Condition).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Eligibility).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasMany(e => e.InventoryPoolItems)
               .WithOne(e => e.Return)
               .HasForeignKey(e => e.ReturnId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryPoolConfiguration : IEntityTypeConfiguration<InventoryPool>
{
    public void Configure(EntityTypeBuilder<InventoryPool> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ProductId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class DemandHistoryConfiguration : IEntityTypeConfiguration<DemandHistory>
{
    public void Configure(EntityTypeBuilder<DemandHistory> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ProductId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Region).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => new { e.ProductId, e.Region });
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class AgentRecommendationConfiguration : IEntityTypeConfiguration<AgentRecommendation>
{
    public void Configure(EntityTypeBuilder<AgentRecommendation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AgentName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Recommendation).HasMaxLength(2000).IsRequired();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class MatchAgentResultConfiguration : IEntityTypeConfiguration<MatchAgentResult>
{
    public void Configure(EntityTypeBuilder<MatchAgentResult> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ProductId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Condition).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Recommendation).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SalePrice).HasPrecision(18, 2);
        builder.Property(e => e.ResaleMargin).HasPrecision(18, 2);
        builder.Property(e => e.ResaleServiceFee).HasPrecision(18, 2);
        builder.Property(e => e.Co2Value).HasPrecision(18, 2);
        builder.Property(e => e.NetValue).HasPrecision(18, 2);
        builder.Property(e => e.Explanation).HasMaxLength(4000);
        builder.Property(e => e.MatchDetailsJson).HasColumnType("nvarchar(max)");

        builder.HasOne(e => e.ReturnRequest)
               .WithMany()
               .HasForeignKey(e => e.ReturnRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ReturnRequestId);
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => new { e.Location, e.Category });
        builder.HasIndex(e => e.MatchScore);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
