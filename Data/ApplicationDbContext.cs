using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.CustomerId);
                entity.Property(c => c.CustomerCode).HasMaxLength(20).IsRequired();
                entity.HasIndex(c => c.CustomerCode).IsUnique();

                entity.Property(c => c.CustomerName).HasMaxLength(150).IsRequired();
                entity.Property(c => c.Email).HasMaxLength(150).IsRequired();
                entity.HasIndex(c => c.Email).IsUnique();

                entity.Property(c => c.Phone).HasMaxLength(20).IsRequired();
                entity.HasIndex(c => c.Phone).IsUnique();

                entity.Property(c => c.CompanyName).HasMaxLength(150);
                entity.Property(c => c.Address).HasMaxLength(250);
                entity.Property(c => c.City).HasMaxLength(100);
                entity.Property(c => c.State).HasMaxLength(100);
                entity.Property(c => c.Status).HasMaxLength(50).IsRequired();

                entity.HasOne(c => c.Owner)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(c => c.OwnerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Lead
            builder.Entity<Lead>(entity =>
            {
                entity.HasKey(l => l.LeadId);
                entity.Property(l => l.LeadCode).HasMaxLength(20).IsRequired();
                entity.HasIndex(l => l.LeadCode).IsUnique();

                entity.Property(l => l.LeadName).HasMaxLength(150).IsRequired();
                entity.Property(l => l.Email).HasMaxLength(150).IsRequired();
                entity.Property(l => l.Phone).HasMaxLength(20).IsRequired();
                entity.Property(l => l.CompanyName).HasMaxLength(150);
                entity.Property(l => l.Source).HasMaxLength(50).IsRequired();
                entity.Property(l => l.Status).HasMaxLength(50).IsRequired();
                entity.Property(l => l.Priority).HasMaxLength(20).IsRequired();
                entity.Property(l => l.ExpectedValue).HasColumnType("decimal(18,2)");

                entity.HasOne(l => l.AssignedUser)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(l => l.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(l => l.ConvertedCustomer)
                    .WithMany()
                    .HasForeignKey(l => l.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Opportunity
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasKey(o => o.OpportunityId);
                entity.Property(o => o.OpportunityName).HasMaxLength(150).IsRequired();
                entity.Property(o => o.Stage).HasMaxLength(50).IsRequired();
                entity.Property(o => o.Status).HasMaxLength(50).IsRequired();
                entity.Property(o => o.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                    .WithMany(l => l.Opportunities)
                    .HasForeignKey(o => o.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.AssignedUser)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(o => o.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FollowUp
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasKey(f => f.FollowUpId);
                entity.Property(f => f.Subject).HasMaxLength(200).IsRequired();
                entity.Property(f => f.FollowUpType).HasMaxLength(50).IsRequired();
                entity.Property(f => f.Status).HasMaxLength(50).IsRequired();
                entity.Property(f => f.RelatedType).HasMaxLength(50).IsRequired();

                entity.HasOne(f => f.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(f => f.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Opportunity)
                    .WithMany(o => o.FollowUps)
                    .HasForeignKey(f => f.OpportunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.AssignedUser)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(f => f.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Activity
            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(a => a.ActivityId);
                entity.Property(a => a.Subject).HasMaxLength(200).IsRequired();
                entity.Property(a => a.ActivityType).HasMaxLength(50).IsRequired();
                entity.Property(a => a.Status).HasMaxLength(50).IsRequired();

                entity.HasOne(a => a.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(a => a.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AssignedUser)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(a => a.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AuditLog
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);
                entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
                entity.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
                entity.Property(a => a.RecordId).HasMaxLength(100);
                entity.Property(a => a.Result).HasMaxLength(50).IsRequired();
                entity.Property(a => a.IpAddress).HasMaxLength(50);
                entity.Property(a => a.Details).HasMaxLength(500);

                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.Action);

                entity.HasOne(a => a.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
