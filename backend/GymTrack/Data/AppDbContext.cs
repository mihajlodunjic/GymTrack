using GymTrack.Common;
using GymTrack.Entities;
using GymTrack.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.Property(user => user.UpdatedAt)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");

            entity.HasKey(member => member.Id);

            entity.Property(member => member.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(member => member.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(member => member.PhoneNumber)
                .HasMaxLength(50);

            entity.Property(member => member.MembershipCode)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(member => member.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(member => member.MembershipCode)
                .IsUnique();

            entity.HasOne(member => member.User)
                .WithOne(user => user.Member)
                .HasForeignKey<Member>(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.ToTable("MembershipPlans");

            entity.HasKey(plan => plan.Id);

            entity.Property(plan => plan.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(plan => plan.Description)
                .HasMaxLength(1000);

            entity.Property(plan => plan.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(plan => plan.PlanType)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(plan => plan.IsActive)
                .HasDefaultValue(true);

            entity.HasDiscriminator(plan => plan.PlanType)
                .HasValue<TimeBasedMembershipPlan>(MembershipPlanType.TimeBased)
                .HasValue<VisitBasedMembershipPlan>(MembershipPlanType.VisitBased)
                .HasValue<CombinedMembershipPlan>(MembershipPlanType.Combined);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }

                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
