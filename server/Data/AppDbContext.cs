using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cauldron.Server.Models;

namespace Cauldron.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Household> Households => Set<Household>();
    public DbSet<UserHousehold> UserHouseholds => Set<UserHousehold>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<UserHousehold>().HasKey(x => new { x.UserId, x.HouseholdId });

        b.Entity<UserHousehold>()
            .HasOne(x => x.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<UserHousehold>()
            .HasOne(x => x.Household)
            .WithMany(h => h.Members)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<UserHousehold>()
            .Property(x => x.Role).HasMaxLength(20);
    }
}