using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Recrutement.Models;

namespace Recrutement.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<JobOffer>()
            .HasOne(j => j.CreatedBy)
            .WithMany()
            .HasForeignKey(j => j.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JobApplication>()
            .HasOne(a => a.JobOffer)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JobApplication>()
            .HasOne(a => a.ReviewedBy)
            .WithMany()
            .HasForeignKey(a => a.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JobApplication>()
            .Property(a => a.Status)
            .HasConversion<string>();

        // Une seule candidature par courriel et par offre
        builder.Entity<JobApplication>()
            .HasIndex(a => new { a.JobOfferId, a.Email })
            .IsUnique();
    }
}