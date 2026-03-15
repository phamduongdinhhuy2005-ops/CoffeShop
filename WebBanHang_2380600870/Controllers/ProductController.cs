// Controllers/ProductController.cs
// FIXED & OPTIMIZED:
// 1. SaveImage() không validate file type → có thể upload file độc hại
// 2. SaveImage() không giới hạn file size → có thể upload file quá lớn
// 3. SaveImage() không sanitize tên file → path traversal vulnerability
// 4. Edit: giữ ImageUrl cũ khi không upload ảnh mới nhưng logic hơi confusing
// 5. Detail: gọi GetAllAsync() 2 lần → dùng 1 query duy nhất

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

        // FIX: Danh sách MIME types được phép upload
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
        };

        // FIX: Giới hạn 5MB
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

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

            // FIX: Dùng 1 lần GetAllAsync và filter trong memory
            var allProducts = await _productRepository.GetAllAsync();
            var productList = allProducts.ToList();

            var related = productList
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4).ToList();

            if (related.Count < 4)
            {
                var extra = productList
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
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("DiscountPercent");

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // FIX: Validate trước khi lưu
                    var validationError = ValidateImageFile(ImageFile);
                    if (validationError != null)
                    {
                        ModelState.AddModelError("ImageFile", validationError);
                        var cats = await _categoryRepository.GetAllAsync();
                        ViewBag.Categories = new SelectList(cats, "Id", "Name");
                        return View(product);
                    }
                    product.ImageUrl = await SaveImage(ImageFile);
                }

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
                {
                    // FIX: Validate image file
                    var validationError = ValidateImageFile(ImageFile);
                    if (validationError != null)
                    {
                        ModelState.AddModelError("ImageFile", validationError);
                        ViewBag.Categories = await _categoryRepository.GetAllAsync();
                        return View(product);
                    }

                    // FIX: Xóa ảnh cũ nếu là file local (không phải URL ngoài)
                    DeleteOldLocalImage(existing.ImageUrl);
                    existing.ImageUrl = await SaveImage(ImageFile);
                }
                else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    existing.ImageUrl = product.ImageUrl;
                }
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

            // FIX: Xóa ảnh local khi xóa sản phẩm
            if (product != null)
                DeleteOldLocalImage(product.ImageUrl);

            await _productRepository.DeleteAsync(id);
            TempData["Success"] = $"Đã xóa sản phẩm \"{name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════
        //  Helper — validate ảnh upload
        // ══════════════════════════════════════════════════════
        private static string? ValidateImageFile(IFormFile file)
        {
            if (file.Length > MaxImageSizeBytes)
                return "Ảnh không được vượt quá 5MB.";

            if (!AllowedMimeTypes.Contains(file.ContentType))
                return "Chỉ chấp nhận ảnh định dạng JPG, PNG, WebP hoặc GIF.";

            // FIX: Kiểm tra extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext))
                return "File không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png, .webp, .gif";

            return null;
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

            // FIX: Chỉ dùng Guid + extension, KHÔNG dùng tên gốc của file (bảo mật)
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var savePath = Path.Combine(imagesFolder, uniqueFileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
                await image.CopyToAsync(fs);

            return "/images/" + uniqueFileName;
        }

        // ══════════════════════════════════════════════════════
        //  Helper — xóa ảnh local (không xóa URL ngoài)
        // ══════════════════════════════════════════════════════
        private void DeleteOldLocalImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/"))
                return; // Không xóa URL ngoài (Google, CDN...)

            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRoot, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch
            {
                // Không throw nếu xóa file thất bại — không critical
            }
        }
    }
}
