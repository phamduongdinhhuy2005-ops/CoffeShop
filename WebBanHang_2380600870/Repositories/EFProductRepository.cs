// Repositories/EFProductRepository.cs
// FIX IDENTITY_INSERT: Dùng ExecutionStrategy.ExecuteAsync để tắt retry logic của SQL Server.

using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Services;

namespace WebBanHang_2380600870.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IdGeneratorService _idGen;

        public EFProductRepository(ApplicationDbContext context, IdGeneratorService idGen)
        {
            _context = context;
            _idGen = idGen;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            product.Id = await _idGen.NextProductIdAsync();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Products ON");
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Products OFF");
                await transaction.CommitAsync();
            });

            await _idGen.ReseedProductsAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                if (product.Images != null && product.Images.Any())
                    _context.ProductImages.RemoveRange(product.Images);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}