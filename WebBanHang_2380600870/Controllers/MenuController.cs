// Controllers/MenuController.cs
// FIX BUG FILTER:
// Khi navigate từ Product/Detail back về /Menu?categoryId=X, Model chỉ chứa
// sản phẩm của category đó → @Model.Count() và catCount tính SAI cho toàn bộ tab.
// Fix: truyền ViewBag.AllProducts riêng để View tính count độc lập với filter.

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

        // GET: /Menu  hoặc  /Menu?categoryId=2
        public async Task<IActionResult> Index(int? categoryId)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var allCategories = await _categoryRepository.GetAllAsync();

            var allProductsList = allProducts.ToList();

            var filtered = categoryId.HasValue
                ? allProductsList.Where(p => p.CategoryId == categoryId.Value).ToList()
                : allProductsList;

            ViewBag.Categories = allCategories;
            ViewBag.ActiveCategory = categoryId;

            // FIX: truyền TOÀN BỘ sản phẩm qua ViewBag để View tính count đúng.
            // Model (filtered) chỉ dùng cho initial render, count phải từ allProducts.
            ViewBag.AllProducts = allProductsList;

            return View(filtered);
        }
    }
}