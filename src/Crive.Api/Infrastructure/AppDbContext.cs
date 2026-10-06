using Crive.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Crive.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<BlockEvent> BlockEvents => Set<BlockEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters for multi-tenancy
        modelBuilder.Entity<Sector>().HasQueryFilter(e => e.TenantId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<Machine>().HasQueryFilter(e => e.TenantId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<Rule>().HasQueryFilter(e => e.TenantId == tenantContext.CurrentTenantId);
        modelBuilder.Entity<BlockEvent>().HasQueryFilter(e => e.TenantId == tenantContext.CurrentTenantId);

        // JSONB column for Rule.Domains
        modelBuilder.Entity<Rule>()
            .Property(e => e.Domains)
            .HasColumnType("jsonb");

        // Indexes
        modelBuilder.Entity<Tenant>().HasIndex(e => e.Slug).IsUnique();
        modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique();
        modelBuilder.Entity<Machine>().HasIndex(e => e.HardwareFingerprint);
        modelBuilder.Entity<BlockEvent>().HasIndex(e => e.BlockedAt);

        // Store enums as strings (to match seed.sql VARCHAR columns)
        modelBuilder.Entity<User>()
            .Property(e => e.Role)
            .HasConversion<string>();
        modelBuilder.Entity<Machine>()
            .Property(e => e.ConnectionStatus)
            .HasConversion<string>();
        modelBuilder.Entity<Rule>()
            .Property(e => e.ScopeType)
            .HasConversion<string>();
        modelBuilder.Entity<Rule>()
            .Property(e => e.Action)
            .HasConversion<string>();
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add tenant info automatically to new entities
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity is Sector s && s.TenantId == Guid.Empty) s.TenantId = tenantContext.CurrentTenantId;
            if (entry.Entity is User u && u.TenantId == Guid.Empty) u.TenantId = tenantContext.CurrentTenantId;
            if (entry.Entity is Machine m && m.TenantId == Guid.Empty) m.TenantId = tenantContext.CurrentTenantId;
            if (entry.Entity is Rule r && r.TenantId == Guid.Empty) r.TenantId = tenantContext.CurrentTenantId;
            if (entry.Entity is BlockEvent b && b.TenantId == Guid.Empty) b.TenantId = tenantContext.CurrentTenantId;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
