// Repositories/EFCategoryRepository.cs
// FIX IDENTITY_INSERT: Dùng ExecutionStrategy.ExecuteAsync để tắt retry logic của SQL Server,
// bắt buộc SET IDENTITY_INSERT và SaveChanges chạy trong cùng 1 transaction/connection.

using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Services;

namespace WebBanHang_2380600870.Repositories
{
    public class EFCategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IdGeneratorService _idGen;

        public EFCategoryRepository(ApplicationDbContext context, IdGeneratorService idGen)
        {
            _context = context;
            _idGen = idGen;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Products)
                .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Category category)
        {
            category.Id = await _idGen.NextCategoryIdAsync();

            // FIX: Dùng ExecutionStrategy để tắt retry, ép toàn bộ chạy trong 1 transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Categories ON");
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Categories OFF");
                await transaction.CommitAsync();
            });

            await _idGen.ReseedCategoriesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category != null)
            {
                if (category.Products != null && category.Products.Any())
                {
                    foreach (var prod in category.Products)
                    {
                        if (prod.Images != null && prod.Images.Any())
                            _context.ProductImages.RemoveRange(prod.Images);
                    }
                    _context.Products.RemoveRange(category.Products);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}