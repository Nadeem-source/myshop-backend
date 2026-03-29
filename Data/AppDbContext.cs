using Microsoft.EntityFrameworkCore;
using RegisterApi.Models;
using RegisterApi.Dtos;

namespace RegisterApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        /* ===================== DB SETS ===================== */

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<SellerMessage> SellerMessages { get; set; }

        /* ===================== MODEL CONFIG ===================== */

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* ========== ORDER → ORDER ITEMS RELATION ========== */

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ SQL Server safe

            /* ========== DECIMAL PRECISION FIX ========== */

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.Price)
                .HasPrecision(18, 2);

            /* ========== IGNORE DTOs (VERY IMPORTANT) ========== */

            modelBuilder.Ignore<AddProductDto>(); // 🔥 THIS FIXES YOUR ERROR
        }
    }
}
