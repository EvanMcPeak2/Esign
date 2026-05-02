using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PdfSigning.Web.Models;
using PdfSigning.Web.Models.Documents;

namespace PdfSigning.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<SignatureField> SignatureFields => Set<SignatureField>();

    public DbSet<SigningSession> SigningSessions => Set<SigningSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.OriginalFileName)
                .HasMaxLength(260)
                .IsRequired();

            entity.Property(x => x.ContentType)
                .HasMaxLength(100);

            entity.Property(x => x.StorageKey)
                .HasMaxLength(500);

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.CompletedAtUtc);

            entity.Property(x => x.OwnerUserId)
                .IsRequired();

            entity.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.SignatureFields)
                .WithOne(x => x.Document)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(x => x.SigningSessions)
                .WithOne(x => x.Document)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SigningSession>(entity =>
        {
            entity.ToTable("SigningSessions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RecipientEmail)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(x => x.AccessTokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasIndex(x => x.AccessTokenHash)
                .IsUnique();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            entity.Property(x => x.CompletedAtUtc);

            entity.Property(x => x.SignedByName)
                .HasMaxLength(200);

            entity.Property(x => x.RevokedAtUtc);

            entity.HasMany(x => x.SignatureFields)
                .WithOne(x => x.SigningSession)
                .HasForeignKey(x => x.SigningSessionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SignatureField>(entity =>
        {
            entity.ToTable("SignatureFields");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Label)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.PageNumber)
                .IsRequired();

            entity.Property(x => x.X)
                .HasPrecision(18, 4)
                .IsRequired();

            entity.Property(x => x.Y)
                .HasPrecision(18, 4)
                .IsRequired();

            entity.Property(x => x.Width)
                .HasPrecision(18, 4)
                .IsRequired();

            entity.Property(x => x.Height)
                .HasPrecision(18, 4)
                .IsRequired();

            entity.Property(x => x.IsRequired)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();
        });
    }
}
