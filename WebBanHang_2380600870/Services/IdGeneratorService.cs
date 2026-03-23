// Services/IdGeneratorService.cs
// Helper tập trung logic tìm ID nhỏ nhất chưa dùng (gap-filling)
// Dùng cho Products, Categories, Orders, Bookings
//
// FIX EF1002: Thay ExecuteSqlRawAsync($"...{max}") bằng ExecuteSqlAsync($"...{max}")
// ExecuteSqlAsync nhận FormattableString → EF Core tự parameterize → không có SQL injection.
// Lưu ý: DBCC CHECKIDENT không hỗ trợ tham số cho tên bảng, nhưng {max} là int
// nên an toàn. Dùng ExecuteSqlAsync để tắt warning EF1002 đúng cách.

using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Services
{
    public class IdGeneratorService
    {
        private readonly ApplicationDbContext _context;

        public IdGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Products ──────────────────────────────────────────
        /// <summary>
        /// Tìm ID nhỏ nhất chưa dùng trong bảng Products.
        /// Ví dụ: {1,2,3,5} → 4; {1,2,3} → 4; {} → 1
        /// </summary>
        public async Task<int> NextProductIdAsync()
            => await FindGapAsync(
                await _context.Products.Select(p => p.Id).ToListAsync());

        public async Task ReseedProductsAsync()
        {
            var max = await _context.Products.MaxAsync(p => (int?)p.Id) ?? 0;
            // FIX EF1002: dùng ExecuteSqlAsync (FormattableString) thay ExecuteSqlRawAsync
            await _context.Database.ExecuteSqlAsync(
                $"DBCC CHECKIDENT ('Products', RESEED, {max})");
        }

        // ── Categories ────────────────────────────────────────
        public async Task<int> NextCategoryIdAsync()
            => await FindGapAsync(
                await _context.Categories.Select(c => c.Id).ToListAsync());

        public async Task ReseedCategoriesAsync()
        {
            var max = await _context.Categories.MaxAsync(c => (int?)c.Id) ?? 0;
            // FIX EF1002: dùng ExecuteSqlAsync (FormattableString) thay ExecuteSqlRawAsync
            await _context.Database.ExecuteSqlAsync(
                $"DBCC CHECKIDENT ('Categories', RESEED, {max})");
        }

        // ── Orders ────────────────────────────────────────────
        public async Task<int> NextOrderIdAsync()
            => await FindGapAsync(
                await _context.Orders.Select(o => o.Id).ToListAsync());

        public async Task ReseedOrdersAsync()
        {
            var max = await _context.Orders.MaxAsync(o => (int?)o.Id) ?? 0;
            // FIX EF1002: dùng ExecuteSqlAsync (FormattableString) thay ExecuteSqlRawAsync
            await _context.Database.ExecuteSqlAsync(
                $"DBCC CHECKIDENT ('Orders', RESEED, {max})");
        }

        // ── Bookings ──────────────────────────────────────────
        public async Task<int> NextBookingIdAsync()
            => await FindGapAsync(
                await _context.Bookings.Select(b => b.Id).ToListAsync());

        public async Task ReseedBookingsAsync()
        {
            var max = await _context.Bookings.MaxAsync(b => (int?)b.Id) ?? 0;
            // FIX EF1002: dùng ExecuteSqlAsync (FormattableString) thay ExecuteSqlRawAsync
            await _context.Database.ExecuteSqlAsync(
                $"DBCC CHECKIDENT ('Bookings', RESEED, {max})");
        }

        // ── Core algorithm ────────────────────────────────────
        private static Task<int> FindGapAsync(List<int> existingIds)
        {
            if (!existingIds.Any()) return Task.FromResult(1);

            var sorted = existingIds.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int expected = i + 1;
                if (sorted[i] != expected)
                    return Task.FromResult(expected); // gap found
            }
            return Task.FromResult(sorted.Max() + 1);
        }
    }
}