using Microsoft.EntityFrameworkCore;
using DukeShopBackend.Models;
using DukeShopBackend.Services;

namespace DukeShopBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(l => l.Product)
                .WithMany(p => p.InventoryLogs)
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(l => l.User)
                .WithMany(u => u.InventoryLogs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Áo", Description = "Áo thun, sơ mi, áo khoác thời trang" },
                new Category { Id = 2, Name = "Quần", Description = "Quần Jeans, kaki, quần short nam nữ" },
                new Category { Id = 3, Name = "Giày dép", Description = "Giày sneaker, giày thể thao & dép" },
                new Category { Id = 4, Name = "Phụ kiện", Description = "Balo laptop, mũ lưỡi trai, túi xách" }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "Lê Gia Hoàng",
                    Username = "quanly",
                    Email = "quanly@dukeshop.vn",
                    Phone = "0901234567",
                    PasswordHash = PasswordSecurity.HashPasswordWithFixedSalt("DukeShop@2026", "DukeShopSeedSalt1"),
                    Role = "ADMIN",
                    CreatedAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    FullName = "Nguyễn Văn A",
                    Username = "nhanvien01",
                    Email = "nhanvien01@dukeshop.vn",
                    Phone = "0987654321",
                    PasswordHash = PasswordSecurity.HashPasswordWithFixedSalt("NhanVien@123", "DukeShopSeedSalt2"),
                    Role = "STAFF",
                    CreatedAt = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc)
                }
            );

            var seedTime = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = "SP01", Name = "Áo Thun Polo Duke Classic", CategoryId = 1, Price = 250000, Quantity = 30, UpdatedAt = seedTime },
                new Product { Id = "SP02", Name = "Áo Khoác Bomber Nam Đen", CategoryId = 1, Price = 480000, Quantity = 4, UpdatedAt = seedTime },
                new Product { Id = "SP03", Name = "Quần Jeans Slimfit Xanh Đậm", CategoryId = 2, Price = 390000, Quantity = 15, UpdatedAt = seedTime },
                new Product { Id = "SP04", Name = "Giày Sneaker Duke Trắng", CategoryId = 3, Price = 650000, Quantity = 0, UpdatedAt = seedTime },
                new Product { Id = "SP05", Name = "Balo Laptop Chống Nước", CategoryId = 4, Price = 320000, Quantity = 12, UpdatedAt = seedTime },
                new Product { Id = "SP06", Name = "Áo Sơ Mi Oxford Trắng", CategoryId = 1, Price = 290000, Quantity = 22, UpdatedAt = seedTime },
                new Product { Id = "SP07", Name = "Quần Short Kaki Basic", CategoryId = 2, Price = 210000, Quantity = 3, UpdatedAt = seedTime },
                new Product { Id = "SP08", Name = "Mũ Lưỡi Trai Thêu Logo Duke", CategoryId = 4, Price = 150000, Quantity = 18, UpdatedAt = seedTime }
            );

            modelBuilder.Entity<InventoryLog>().HasData(
                new InventoryLog
                {
                    Id = 1,
                    ProductId = "SP01",
                    ProductNameSnapshot = "Áo Thun Polo Duke Classic",
                    UserId = 1,
                    ActionType = "IMPORT",
                    QuantityChange = 30,
                    Note = "Nhập lô hàng đầu tháng 11",
                    CreatedAt = new DateTime(2026, 11, 1, 9, 30, 0, DateTimeKind.Utc)
                },
                new InventoryLog
                {
                    Id = 2,
                    ProductId = "SP02",
                    ProductNameSnapshot = "Áo Khoác Bomber Nam Đen",
                    UserId = 1,
                    ActionType = "EXPORT",
                    QuantityChange = -6,
                    Note = "Xuất đơn sỉ chi nhánh Hà Đông",
                    CreatedAt = new DateTime(2026, 11, 5, 14, 15, 0, DateTimeKind.Utc)
                },
                new InventoryLog
                {
                    Id = 3,
                    ProductId = "SP05",
                    ProductNameSnapshot = "Balo Laptop Chống Nước",
                    UserId = 1,
                    ActionType = "IMPORT",
                    QuantityChange = 12,
                    Note = "Nhập bổ sung phụ kiện mùa tựu trường",
                    CreatedAt = new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
