// Controllers/ProductController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Repositories;

namespace WebBanHang_2380600870.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _env;

        public ProductController(IProductRepository productRepository,
                                 ICategoryRepository categoryRepository,
                                 IWebHostEnvironment env)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _env = env;
        }

        // ══════════════════════════════════════════════════════
        //  PUBLIC — xem chi tiết sản phẩm
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var allProducts = await _productRepository.GetAllAsync();
            var related = allProducts
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4).ToList();

            if (related.Count < 4)
            {
                var extra = allProducts
                    .Where(p => p.CategoryId != product.CategoryId && p.Id != id)
                    .Take(4 - related.Count).ToList();
                related.AddRange(extra);
            }

            return View(new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related
            });
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN — danh sách
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN — thêm sản phẩm
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile? ImageFile)
        {
            // FIX: Xóa navigation properties không bind từ form khỏi ModelState
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("DiscountPercent");

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                    product.ImageUrl = await SaveImage(ImageFile);

                await _productRepository.AddAsync(product);
                TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN — xem chi tiết (admin view)
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN — chỉnh sửa
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? ImageFile)
        {
            // FIX: Xóa navigation properties không bind từ form khỏi ModelState
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("DiscountPercent");

            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _productRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.Description = product.Description;
                existing.CategoryId = product.CategoryId;
                existing.IsOnSale = product.IsOnSale;
                existing.DiscountPercent = product.DiscountPercent;

                if (ImageFile != null && ImageFile.Length > 0)
                    existing.ImageUrl = await SaveImage(ImageFile);
                else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                    existing.ImageUrl = product.ImageUrl;
                // Nếu cả hai đều trống → giữ nguyên existing.ImageUrl

                await _productRepository.UpdateAsync(existing);
                TempData["Success"] = $"Đã cập nhật sản phẩm \"{existing.Name}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id) => RedirectToAction(nameof(Edit), new { id });

        // ══════════════════════════════════════════════════════
        //  ADMIN — xóa sản phẩm
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            string name = product?.Name ?? $"#{id}";
            await _productRepository.DeleteAsync(id);
            TempData["Success"] = $"Đã xóa sản phẩm \"{name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════
        //  Helper — lưu ảnh upload
        // ══════════════════════════════════════════════════════
        private async Task<string> SaveImage(IFormFile image)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagesFolder = Path.Combine(webRoot, "images");
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            var savePath = Path.Combine(imagesFolder, uniqueFileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
                await image.CopyToAsync(fs);

            return "/images/" + uniqueFileName;
        }
    }
}
