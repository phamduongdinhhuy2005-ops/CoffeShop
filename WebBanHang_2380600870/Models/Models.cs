// Models/Models.cs
// THAY THẾ TOÀN BỘ FILE CŨ
// FIXED & OPTIMIZED:
// 1. OrderDetail.ItemNote — lưu Size/Đường/Topping
// 2. Order.PaymentMethod — lưu phương thức thanh toán
// 3. AppUser.CreatedAt dùng UtcNow
// 4. Subscriber unique index
// 5. Booking.Phone regex validate SĐT Việt Nam
// [MỚI] 6. Booking.UserId (nullable FK → AppUser) — liên kết booking với tài khoản
// [MỚI] 7. AppUser.Bookings navigation property
// [MỚI] 8. OnModelCreating: cấu hình Booking → AppUser (SetNull on delete)
// SAU KHI THAY FILE NÀY: chạy migration
//   dotnet ef migrations add AddBookingUserId
//   dotnet ef database update

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang_2380600870.Models
{
    public class Subscriber
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = "";

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }

    // ===================== PRODUCT =====================
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(100, ErrorMessage = "Tên không quá 100 ký tự")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(0.01, 10000000, ErrorMessage = "Giá phải từ 0.01 đến 10,000,000")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public IEnumerable<ProductImage> Images { get; set; } = null!;

        // ── Khuyến mãi / Sale ────────────────────────────
        public bool IsOnSale { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Phần trăm giảm phải từ 0 đến 100")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercent { get; set; }

        /// <summary>Giá sau giảm — tính toán, không lưu DB</summary>
        [NotMapped]
        public decimal SalePrice =>
            IsOnSale && DiscountPercent.HasValue && DiscountPercent > 0
                ? Math.Round(Price * (1 - DiscountPercent.Value / 100m), 0)
                : Price;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }

    // ===================== CATEGORY =====================
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(50, ErrorMessage = "Tên không quá 50 ký tự")]
        public string Name { get; set; } = null!;

        public IEnumerable<Product> Products { get; set; } = null!;
    }

    // ===================== PRODUCT IMAGE =====================
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public string Url { get; set; } = null!;

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }

    // ===================== APP USER =====================
    public class AppUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        // FIX: Dùng UtcNow thay vì Now để tránh timezone issues
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Order>? Orders { get; set; }

        // [MỚI] Navigation property — để query bookings của user này
        public List<Booking>? Bookings { get; set; }
    }

    // ===================== ORDER =====================
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        Ready,
        Completed,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public AppUser? User { get; set; }

        // FIX: Dùng UtcNow
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string? Note { get; set; }
        public string? ShippingAddress { get; set; }

        // FIX: Lưu phương thức thanh toán
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "COD";

        public List<OrderDetail>? OrderDetails { get; set; }
    }

    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // FIX: Lưu ghi chú tùy chọn (Size, Đường, Topping) cho từng item
        [StringLength(500)]
        public string? ItemNote { get; set; }
    }

    // ===================== BOOKING =====================
    public enum BookingStatus { Pending, Confirmed, Cancelled }

    public class Booking
    {
        public int Id { get; set; }

        // [MỚI] Liên kết booking với tài khoản user
        // Nullable: khách không đăng nhập vẫn đặt được, khi đã login sẽ có UserId
        // Khi user bị xóa: UserId sẽ thành NULL (SetNull), booking vẫn giữ lại
        public string? UserId { get; set; }
        public AppUser? User { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(256)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        // FIX: Validate SĐT Việt Nam (0xxx hoặc +84xxx)
        [RegularExpression(@"^(0|\+84)[3-9][0-9]{8}$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ (VD: 0912345678)")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Ngày đặt không được để trống")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Giờ đến không được để trống")]
        public string BookingTime { get; set; } = null!;

        [Range(1, 20, ErrorMessage = "Số khách từ 1 đến 20")]
        public int GuestCount { get; set; } = 1;

        [StringLength(100)]
        public string? Occasion { get; set; }

        [StringLength(500)]
        public string? SpecialRequest { get; set; }

        // FIX: Dùng UtcNow
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
    }

    // ===================== DB CONTEXT =====================
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Subscriber> Subscribers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.DiscountPercent)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // [MỚI] Booking → AppUser: nullable FK, xóa user thì UserId thành NULL
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // FIX: Unique index cho Subscriber.Email
            modelBuilder.Entity<Subscriber>()
                .HasIndex(s => s.Email)
                .IsUnique();

            // ===== SEED CATEGORIES =====
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Trà & Thảo Mộc" },
                new Category { Id = 2, Name = "Cà Phê" },
                new Category { Id = 3, Name = "Bánh Sáng" }
            );

            // ===== SEED PRODUCTS =====
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, CategoryId = 1, Name = "Trà Hoa Cúc Mật Ong", Price = 55000, Description = "Hoa cúc hữu cơ dịu nhẹ, pha cùng mật ong rừng địa phương và một chút vani.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCEAjGgjkSer5q9ba5D6NyzN1c9lTOJPMXWpNJltT_GLw0NEh93dsoZsJaXB20ZNlnBukK0BvyNWGB6fwjYu-mP17nG4ALpaDwiVOTRHcQvCf1w1Ib5UbFX9y76GFKvPekabC6V_VgoEm-Zc1Y_S96uZErX-tYH5XAeDCbc7TxftVTD-rmBHuOd7aSkx881C8NtH-mc5rMhj4dyuSomr4y-1PPGwQdiN893dwpZjGmWyehq9XIq_Ba8NDWvScXE1Tf9RBlP3LwPkGkJ" },
                new Product { Id = 2, CategoryId = 1, Name = "Trà Hoa Atiso Đỏ", Price = 58000, Description = "Cánh atiso đỏ giàu vitamin, kết hợp hoa hồng Bulgaria khô cho hương thơm thanh nhẹ.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBquJBlkgANzMfyJGh1XkspjUt3umtwxQzexDd8I0mt0FgPpx6FXkExK_M7Xm-wM6-LP7BG6Uy4DlU4Zs_SLn4nniVk8-ZjKvOweTbUCwoZ4qlVJ3Ng2_hYfQ3E6HoYycQvSScsSUO1HRJnZF3sizIF_rZ_Dy-byN31UV3fduUnRR-A1zsaH7m7UztM6dHj1VAVWReoWicfHForfNH45bxgCGWHUAofQXMqeY9jauQltovivmnmKywndNVfLUxF3nPKs7Lg3ozOwc5Re" },
                new Product { Id = 3, CategoryId = 1, Name = "Earl Grey Hoàng Gia", Price = 55000, Description = "Trà đen thượng hạng ướp tinh dầu bergamot và cánh hoa ngô xanh — cổ điển và tinh tế.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuD-HrH2opygixRAPA4_hKWugp4IUUWQdv8LY4AAqkAJ4eUISM5MqQu2ZqCQaD_fj6h5ujbIB7gwlMDgzTEGuN5-L7j_J49jf5Y6my037ZvILcU-yaOt5EGfRdq-mRf5ILDr5o8oYHvOHa4-EBvZbGfClfC1TQjlYMdoHZWYEb7qY9tlI0CbLCoHI4MXnOdGG8P02A8BEg1Y9ZhMI8m8EC_B-7UvsW1IxAKATnSeQzc2Iava4vUQGKDILPm_YIYx-pZDsddI8-ILgzY6" },
                new Product { Id = 4, CategoryId = 1, Name = "Trà Lài Ngọc Châu", Price = 65000, Description = "Trà xanh cuộn tay, ướp hoa lài tươi nhiều lần — hương thơm dịu mà sâu lắng.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuDQqi2HYPgIa_kB6UkOUsv1ob9VljdNcRiPmydMW8pDePwjEl8EvWaBKZd6VTeetyXv31Jee_gx0IAf5xy6B129-6OEQNi9FUFN42I4CkNVtsJrZGES7TJSBdXDHFauYzCKd1YEq1oUKfqEsfs8n-n0sw2wy7TzjnveLWlJLQyBiw-geTWQg_J8kRLA7hpIo5euMXC5oppW3jMTIKImMRauGsxp1-19-wTW6LG1IosZWTlhpYXUMLc58UBBXuKlsIxRHhTJuu-0i5oU" },
                new Product { Id = 5, CategoryId = 2, Name = "Cà Phê Trứng Hà Nội", Price = 65000, Description = "Robusta đậm đà, phủ lớp lòng đỏ trứng đánh bông cùng sữa đặc — đặc sản không thể thiếu.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBtk6EC-fylhVI5xH_hiJ8RDZU-69cXHO2s1AehaDaLNDlCJYITX2Zxb1Z9FjqLPonF1rKl5UbilrIOwd10DxLcnVcB1sYqKd-q-cBzkMML0GEQK84wxT31BEbpLz89Raqu3e8S6B6y6eyfYz8NMNZ6uy86k8v6sEjh0KsoEA_9mz8VGql5gajQZ0kafubfK55b95S9AjEflczQl7mYmLhBnNE6M70ea_tSmPbAHHcZTKFHTypRezqxN7Cm--NywSQi8Su4cmDVEECJ" },
                new Product { Id = 6, CategoryId = 2, Name = "Espresso Truyền Thống", Price = 35000, Description = "Blend Arabica & Robusta, chiết xuất chuẩn mực — crema vàng óng, đậm mà không gắt.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBAgg0VukbL8PkMBh630VrgmEo5GlHTpzYcPsve8JYTreMPfDcsUXIpEGgQxW_Mw_xFs2A7aPigCqb8WBVfvYVbkQRIfQPihVRu9nSX8LVqBRs9-Bx-EfvJPEypDdb9Eh2IsKp9QGrGb-Ltj40PMoFo15yqvF2Vgf6mwzVUQ9f52k5I_UIbONcIO9eWA9PwlzcgN8OJR38B-a5VQ-CXMapEAYOQf3-ibyb8jEm4anTRkFjkDMIoZkEEOBTMLQzS0d8SfCcvfAX_0qKO" },
                new Product { Id = 7, CategoryId = 2, Name = "Cold Brew 18 Giờ", Price = 50000, Description = "Ủ lạnh suốt 18 tiếng — vị mượt mà, ít đắng, thoảng hương sô-cô-la nhẹ nhàng.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAmJUt1qccm5508I5vD_7QhOCniGdlCnerKYHZODhzkWxHJIA_t1SglSieq6JrPBXjg0YRwJjOIHzaOhaFNsdpo5mt7bABv4IHhr2iFalxHhNNpghXL3EFetndNXXGE0AHca9SvXHiTeroRThJ7BlNPHifK1w01nGqKZ4eFo5mDoYOy6hp-ozZpOBnEC4yJRCbrCkxAmYn2Q8lCulkG1zehU5QCcNhiciqPVQSKmkDozC9BtdGSfRAGLxqbSOO6xXQk4iCT6-5RAb_r" },
                new Product { Id = 8, CategoryId = 2, Name = "Latte Nhung", Price = 45000, Description = "Sữa tươi đánh bọt mịn như nhung, rót trên hai shot espresso — thêm chút quế cho ấm lòng.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAtK1_y4hfIsSD370bZL11idnq18mh0bc-3O46w-21oCAtSxTFfm17M9PS-ii40W8IhN967PdrnBbmx5P4Ra2fYE4vQCZ99yVEEWSXMCnw4Fltbt_F8pBkmhOEfDtILMliJIzUQ0pOult2Rm8kqS2WR-p7MxwYFeInvMr8vCXChS_YafKfUERTuBmkmpE6fc7zxbuUFXv3u4s3QEXLmUWB1RJtPU2-4tPeNr-CkaBdM21mDlNgrLLo06F_Nd08Bw5PsEuxutMyn6316" },
                new Product { Id = 9, CategoryId = 3, Name = "Croissant Hạnh Nhân", Price = 38000, Description = "Nướng hai lần, nhân kem hạnh nhân, rắc hạnh nhân lát lên trên — giòn tan và thơm lừng.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuDuXIB-ebcP8NsQJpZGJoF555uRr-TrEgQ_s7NJOUSSS9-HqIXFAHZeJtkttWZfQ8sPueSPg_0ws4eiuadUjFUW1N1ErYvkqhQPGgGsdAssgzGXgux5wXpORtpK6Bduqw8IeBZaJIRfArYsKVdSUGAEYh8H4FOgRHP9y3AaqLGZB9n8_0h40-m73xcy2x0UN60LDpOZicMQpeHKmAFax9Nee9jXF_z6gJSvMaXf7cv2qln-xesVIxQazR1xqZrIUgUNI5mg7IqRGeFF" },
                new Product { Id = 10, CategoryId = 3, Name = "Bánh Trái Cây Mùa", Price = 35000, Description = "Mứt trái cây theo mùa trên nền bánh ngàn lớp giòn rụm, phủ kem trứng vani nhẹ nhàng.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCN56R6kgmsS-aVc-eplM5QDI5vQHrjwyeV_szA7HBIFtj7td95uvjPXEvW4uyTU7U5Ef28-H4FAVRwr4A-a-X-Hj_K7UJzCGI_f6_Zw7TEBi23Feimclr93XrVM_PtHX6dkj_UcpWRBbZ8Jd9EFslVSFsxJi_Ltzo-xPWPVgFOt9S1WSvMq1a6alIAMS5-apdbb34SRWlJVU36erUQCTZx05UgWKz1YQKeB1GEhRHjRPUqsK2mn2UWGTfw6JUduGg6WZnmrDJ1xksr" },
                new Product { Id = 11, CategoryId = 3, Name = "Bánh Mì Chua & Mứt", Price = 42000, Description = "Hai lát bánh mì chua thủ công nướng vàng, ăn kèm bơ lên men và mứt dâu rừng tự làm.", ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCFGhQ6PA3PWH7EXeIIw2LwXxKOZWXr6dCzy4iM9UAn8xYcbuUD1uqhYNyQtgzpaavc1tbDt9e11zg6xyih5kuREdjtIqAbcWio4BqKpJ1n4kFpCf8cutPqkeSukUSnvRlNXOCKP_h0h95C2E0Qp89eRaZRbyGSgjL1OUEgnMjmQ4iEeYILEp4uDy0jmrYhv4S4xRUbiYbJmIvsfvRTbI2JkYBZVYWe1_YZLIiq3xokHn46ysYFenexcGQiICdUHPPph4ZuUiwWzipa" }
            );
        }
    }
}
