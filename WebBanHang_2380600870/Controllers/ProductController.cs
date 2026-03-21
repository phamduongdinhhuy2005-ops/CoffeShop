// Controllers/ProductController.cs
// UPDATED: Index() truyền ViewBag.Categories để filter button động trong view

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Repositories;

namespace WebBanHang_2380600870.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public ProductController(IProductRepository productRepository,
                                 ICategoryRepository categoryRepository,
                                 IWebHostEnvironment env,
                                 ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _env = env;
            _context = context;
        }

        // ══════════════════════════════════════════════════════
        //  PUBLIC — xem chi tiết sản phẩm
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

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
            // FIX: Truyền danh mục để view render filter buttons động
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
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
        //  ADMIN — chỉnh sửa GET
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(product);
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN — chỉnh sửa POST (CÓ xử lý extra images)
        // ══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? ImageFile,
            List<IFormFile?>? ExtraImages,
            List<int?>? ExtraImageIds,
            List<string?>? ExtraImageUrls)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("DiscountPercent");
            ModelState.Remove("ExtraImages");
            ModelState.Remove("ExtraImageIds");
            ModelState.Remove("ExtraImageUrls");

            if (id != product.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                return View(product);
            }

            var existing = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing == null) return NotFound();

            // ── Cập nhật thông tin cơ bản ──
            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.Description = product.Description;
            existing.CategoryId = product.CategoryId;
            existing.IsOnSale = product.IsOnSale;
            existing.DiscountPercent = product.DiscountPercent;

            // ── Ảnh chính (slot 0) ──
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var err = ValidateImageFile(ImageFile);
                if (err != null)
                {
                    ModelState.AddModelError("ImageFile", err);
                    ViewBag.Categories = await _categoryRepository.GetAllAsync();
                    return View(product);
                }
                DeleteOldLocalImage(existing.ImageUrl);
                existing.ImageUrl = await SaveImage(ImageFile);
            }
            else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                existing.ImageUrl = product.ImageUrl;
            }

            // ── Ảnh phụ (slots 1-3) ──
            var existingImages = existing.Images?.ToList() ?? new List<ProductImage>();

            for (int i = 0; i < 3; i++)
            {
                var newFile = (ExtraImages != null && i < ExtraImages.Count) ? ExtraImages[i] : null;
                var currentUrl = (ExtraImageUrls != null && i < ExtraImageUrls.Count) ? ExtraImageUrls[i] ?? "" : "";
                var existingImg = i < existingImages.Count ? existingImages[i] : null;

                bool hasNewFile = newFile != null && newFile.Length > 0;
                bool isDeleted = string.IsNullOrEmpty(currentUrl);
                bool isNewUrl = !isDeleted && currentUrl != "new_file" && currentUrl.StartsWith("http");

                if (hasNewFile)
                {
                    var err = ValidateImageFile(newFile!);
                    if (err != null) continue;

                    var newUrl = await SaveImage(newFile!);
                    if (existingImg != null)
                    {
                        DeleteOldLocalImage(existingImg.Url);
                        existingImg.Url = newUrl;
                        _context.ProductImages.Update(existingImg);
                    }
                    else
                    {
                        _context.ProductImages.Add(new ProductImage { ProductId = id, Url = newUrl });
                    }
                }
                else if (isNewUrl)
                {
                    if (existingImg != null)
                    {
                        if (existingImg.Url?.StartsWith("/images/") == true)
                            DeleteOldLocalImage(existingImg.Url);
                        existingImg.Url = currentUrl;
                        _context.ProductImages.Update(existingImg);
                    }
                    else
                    {
                        _context.ProductImages.Add(new ProductImage { ProductId = id, Url = currentUrl });
                    }
                }
                else if (isDeleted)
                {
                    if (existingImg != null)
                    {
                        DeleteOldLocalImage(existingImg.Url);
                        _context.ProductImages.Remove(existingImg);
                    }
                }
                // currentUrl == "new_file" nhưng không có file → giữ nguyên
            }

            _context.Products.Update(existing);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật sản phẩm \"{existing.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
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

            if (product != null)
                DeleteOldLocalImage(product.ImageUrl);

            await _productRepository.DeleteAsync(id);
            TempData["Success"] = $"Đã xóa sản phẩm \"{name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════
        private static string? ValidateImageFile(IFormFile file)
        {
            if (file.Length > MaxImageSizeBytes)
                return "Ảnh không được vượt quá 5MB.";
            if (!AllowedMimeTypes.Contains(file.ContentType))
                return "Chỉ chấp nhận ảnh định dạng JPG, PNG, WebP hoặc GIF.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext))
                return "File không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png, .webp, .gif";
            return null;
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var imagesFolder = Path.Combine(webRoot, "images");
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var savePath = Path.Combine(imagesFolder, uniqueFileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
                await image.CopyToAsync(fs);

            return "/images/" + uniqueFileName;
        }

        private void DeleteOldLocalImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/"))
                return;
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRoot, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch { }
        }
    }
}