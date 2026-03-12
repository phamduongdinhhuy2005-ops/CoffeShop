using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Repositories
{
    public class EFCategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public EFCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories
                .Include(c => c.Products)
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
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
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
                // If category has products, remove their images first, then remove products
                if (category.Products != null && category.Products.Any())
                {
                    foreach (var prod in category.Products)
                    {
                        if (prod.Images != null && prod.Images.Any())
                        {
                            _context.ProductImages.RemoveRange(prod.Images);
                        }
                    }

                    _context.Products.RemoveRange(category.Products);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
