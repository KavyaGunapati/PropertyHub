namespace PropertyHub.DataAccess.Context;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Entities;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties { get; set; }
    public DbSet<PropertyImage> PropertyImages { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Favorite> Favorites {get;set;}
    public DbSet<Review> Reviews{get;set;}
    public DbSet<Inquiry> Inquiries{get;set;}
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>()
            .HasOne(p => p.Agent)
            .WithMany(a => a.Properties)
            .HasForeignKey(p => p.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PropertyImage>()
            .HasOne(pi => pi.Property)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for search performance
        builder.Entity<Property>().HasIndex(p => p.City);
        builder.Entity<Property>().HasIndex(p => p.Status);
        builder.Entity<Property>().HasIndex(p => new { p.City, p.Status });
        builder.Entity<Property>().HasIndex(p => p.AgentId);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();
        builder.Entity<Favorite>()
            .HasIndex(f=>new {f.UserId,f.PropertyId})
            .IsUnique();
        builder.Entity<Favorite>()
            .HasOne(f=>f.Property)
            .WithMany()
            .HasForeignKey(f=>f.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Favorite>()
            .HasOne(f=>f.User)
            .WithMany()
            .HasForeignKey(f=>f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Review>()
            .HasIndex(r => new { r.PropertyId, r.UserId })
            .IsUnique();
        builder.Entity<Review>()
            .HasOne(f=>f.User)
            .WithMany()
            .HasForeignKey(f=>f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Review>()
            .HasOne(f=>f.Property)
            .WithMany()
            .HasForeignKey(f=>f.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Inquiry>()
            .HasOne(i => i.Property)
            .WithMany()
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Inquiry>()
           .HasOne(i => i.Buyer)
           .WithMany()
           .HasForeignKey(i => i.BuyerId)
           .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Inquiry>()
           .HasIndex(i => new { i.PropertyId, i.Status, i.CreatedAt });

// Supports the buyer's "my inquiries" list
        builder.Entity<Inquiry>()
           .HasIndex(i => new { i.BuyerId, i.CreatedAt });

    }
}