// Controllers/MenuController.cs
using Microsoft.AspNetCore.Mvc;
using WebBanHang_2380600870.Repositories;

namespace WebBanHang_2380600870.Controllers
{
    public class MenuController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public MenuController(IProductRepository productRepository,
                              ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // GET: /Menu  hoáº·c  /Menu?categoryId=2
        public async Task<IActionResult> Index(int? categoryId)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var allCategories = await _categoryRepository.GetAllAsync();

            var filtered = categoryId.HasValue
                ? allProducts.Where(p => p.CategoryId == categoryId.Value).ToList()
                : allProducts.ToList();

            ViewBag.Categories = allCategories;
            ViewBag.ActiveCategory = categoryId;

            return View(filtered);
        }
    }
}
