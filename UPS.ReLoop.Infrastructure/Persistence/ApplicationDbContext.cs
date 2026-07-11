namespace UPS.ReLoop.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UPS.ReLoop.Domain.Common;
using UPS.ReLoop.Domain.Entities;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Package> Packages => Set<Package>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<InventoryPool> InventoryPool => Set<InventoryPool>();
    public DbSet<DemandHistory> DemandHistory => Set<DemandHistory>();
    public DbSet<AgentRecommendation> AgentRecommendations => Set<AgentRecommendation>();
    public DbSet<MatchAgentResult> MatchAgentResults => Set<MatchAgentResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TrackingNumber).IsUnique();
            entity.Property(e => e.TrackingNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SenderName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SenderAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RecipientName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RecipientAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Weight).HasPrecision(10, 2);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Package)
                  .WithMany(p => p.ReturnRequests)
                  .HasForeignKey(e => e.PackageId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
