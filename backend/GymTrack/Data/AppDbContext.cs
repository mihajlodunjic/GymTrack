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

    public DbSet<MembershipPayment> MembershipPayments => Set<MembershipPayment>();

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<SystemNotification> SystemNotifications => Set<SystemNotification>();

    public DbSet<Student> Students => Set<Student>();

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

        modelBuilder.Entity<MembershipPayment>(entity =>
        {
            entity.ToTable("MembershipPayments");

            entity.HasKey(payment => payment.Id);

            entity.Property(payment => payment.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(payment => payment.PaidAt)
                .IsRequired();

            entity.Property(payment => payment.ValidFrom)
                .IsRequired();

            entity.Property(payment => payment.Note)
                .HasMaxLength(1000);

            entity.Property(payment => payment.CreatedAt)
                .IsRequired();

            entity.Property(payment => payment.UpdatedAt)
                .IsRequired();

            entity.HasOne(payment => payment.Member)
                .WithMany(member => member.MembershipPayments)
                .HasForeignKey(payment => payment.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(payment => payment.MembershipPlan)
                .WithMany(plan => plan.MembershipPayments)
                .HasForeignKey(payment => payment.MembershipPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(payment => payment.CreatedByUser)
                .WithMany(user => user.CreatedMembershipPayments)
                .HasForeignKey(payment => payment.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.ToTable("CheckIns");

            entity.HasKey(checkIn => checkIn.Id);

            entity.Property(checkIn => checkIn.CheckedInAt)
                .IsRequired();

            entity.Property(checkIn => checkIn.WasMembershipValid)
                .IsRequired();

            entity.Property(checkIn => checkIn.Note)
                .HasMaxLength(1000);

            entity.Property(checkIn => checkIn.CreatedAt)
                .IsRequired();

            entity.HasOne(checkIn => checkIn.Member)
                .WithMany(member => member.CheckIns)
                .HasForeignKey(checkIn => checkIn.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(checkIn => checkIn.MembershipPayment)
                .WithMany(payment => payment.CheckIns)
                .HasForeignKey(checkIn => checkIn.MembershipPaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(checkIn => checkIn.CheckedInByUser)
                .WithMany(user => user.RecordedCheckIns)
                .HasForeignKey(checkIn => checkIn.CheckedInByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SystemNotification>(entity =>
        {
            entity.ToTable("SystemNotifications");

            entity.HasKey(notification => notification.Id);

            entity.Property(notification => notification.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(notification => notification.Message)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(notification => notification.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(notification => notification.IsRead)
                .HasDefaultValue(false);

            entity.Property(notification => notification.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");

            entity.HasKey(student => student.Id);

            entity.Property(student => student.Ime)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(student => student.Prezime)
                .HasMaxLength(100)
                .IsRequired();
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
