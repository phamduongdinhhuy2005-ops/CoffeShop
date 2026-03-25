// Controllers/ProductController.cs
// THEM MOI: Action ApiDemo() — trang demo RESTful API Bai 6
// Tat ca chuc nang cu giu nguyen, chi them 1 action o cuoi

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
        private readonly ILogger<ProductController> _logger;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
        };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public ProductController(IProductRepository productRepository,
                                 ICategoryRepository categoryRepository,
                                 IWebHostEnvironment env,
                                 ApplicationDbContext context,
                                 ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _env = env;
            _context = context;
            _logger = logger;
        }

        // PUBLIC — xem chi tiet
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            var allProducts = await _productRepository.GetAllAsync();
            var productList = allProducts.ToList();
            var related = productList.Where(p => p.CategoryId == product.CategoryId && p.Id != id).Take(4).ToList();
            if (related.Count < 4)
                related.AddRange(productList.Where(p => p.CategoryId != product.CategoryId && p.Id != id).Take(4 - related.Count));
            return View(new ProductDetailViewModel { Product = product, RelatedProducts = related });
        }

        // ADMIN — danh sach
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(products);
        }

        // ADMIN — them GET
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        // ADMIN — them POST
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
    Product product,
    IFormFile? ImageFile,
    IFormFile? ExtraFile0,
    IFormFile? ExtraFile1,
    IFormFile? ExtraFile2,
    List<string?>? ExtraImageUrls)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("DiscountPercent");
            ModelState.Remove("ExtraFile0");
            ModelState.Remove("ExtraFile1");
            ModelState.Remove("ExtraFile2");
            ModelState.Remove("ExtraImageUrls");

            if (!ModelState.IsValid)
            {
                var cats = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(cats, "Id", "Name");
                return View(product);
            }

            // ── Anh chinh ──
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var err = ValidateImageFile(ImageFile);
                if (err != null)
                {
                    ModelState.AddModelError("ImageFile", err);
                    var cats = await _categoryRepository.GetAllAsync();
                    ViewBag.Categories = new SelectList(cats, "Id", "Name");
                    return View(product);
                }
                product.ImageUrl = await SaveImage(ImageFile);
            }
            // Neu khong co file thi giu nguyen ImageUrl tu hidden input (URL paste)

            // Luu san pham truoc de co Id
            await _productRepository.AddAsync(product);

            // ── Anh phu (3 slots) ──
            var extraFiles = new IFormFile?[] { ExtraFile0, ExtraFile1, ExtraFile2 };
            for (int i = 0; i < 3; i++)
            {
                var newFile = extraFiles[i];
                var urlFromHidden = (ExtraImageUrls != null && i < ExtraImageUrls.Count)
                    ? (ExtraImageUrls[i] ?? "") : "";

                bool hasFile = newFile != null && newFile.Length > 0;
                bool hasUrl = !string.IsNullOrEmpty(urlFromHidden) && urlFromHidden != "new_file";

                if (hasFile)
                {
                    var err = ValidateImageFile(newFile!);
                    if (err != null)
                    {
                        _logger.LogWarning("Extra slot {Slot} validation failed: {Err}", i, err);
                        continue;
                    }
                    var savedUrl = await SaveImage(newFile!);
                    _context.ProductImages.Add(new ProductImage { ProductId = product.Id, Url = savedUrl });
                }
                else if (hasUrl)
                {
                    _context.ProductImages.Add(new ProductImage { ProductId = product.Id, Url = urlFromHidden });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Da them san pham \"{product.Name}\" thanh cong!";
            return RedirectToAction(nameof(Index));
        }

        // ADMIN — xem chi tiet admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ADMIN — chinh sua GET
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(product);
        }

        // ADMIN — chinh sua POST
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? ImageFile,
            IFormFile? ExtraFile0,
            IFormFile? ExtraFile1,
            IFormFile? ExtraFile2,
            List<int?>? ExtraImageIds,
            List<string?>? ExtraImageUrls)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("DiscountPercent");
            ModelState.Remove("ExtraFile0");
            ModelState.Remove("ExtraFile1");
            ModelState.Remove("ExtraFile2");
            ModelState.Remove("ExtraImageIds");
            ModelState.Remove("ExtraImageUrls");

            if (id != product.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                var rp = await _productRepository.GetByIdAsync(id);
                return View(rp ?? product);
            }

            var existing = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing == null) return NotFound();

            // ── Cap nhat thong tin co ban ──
            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.Description = product.Description;
            existing.CategoryId = product.CategoryId;
            existing.IsOnSale = product.IsOnSale;
            existing.DiscountPercent = product.DiscountPercent;

            // ── Anh chinh ──
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var err = ValidateImageFile(ImageFile);
                if (err != null)
                {
                    TempData["Error"] = $"Anh chinh: {err}";
                    ViewBag.Categories = await _categoryRepository.GetAllAsync();
                    var rp = await _productRepository.GetByIdAsync(id);
                    return View(rp ?? product);
                }
                DeleteOldLocalImage(existing.ImageUrl);
                existing.ImageUrl = await SaveImage(ImageFile);
                _logger.LogInformation("Main image saved: {Url}", existing.ImageUrl);
            }
            else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                if (existing.ImageUrl != product.ImageUrl)
                {
                    if (!string.IsNullOrEmpty(existing.ImageUrl) && existing.ImageUrl.StartsWith("/images/"))
                        DeleteOldLocalImage(existing.ImageUrl);
                    existing.ImageUrl = product.ImageUrl;
                }
            }

            // ── Anh phu (3 slots) ──
            var existingImages = existing.Images?
                .OrderBy(img => img.Id)
                .ToList() ?? new List<ProductImage>();

            var extraFiles = new IFormFile?[] { ExtraFile0, ExtraFile1, ExtraFile2 };

            _logger.LogInformation("ExtraFile0={F0} ExtraFile1={F1} ExtraFile2={F2}",
                ExtraFile0?.Length ?? 0, ExtraFile1?.Length ?? 0, ExtraFile2?.Length ?? 0);
            _logger.LogInformation("ExtraImageUrls: {Urls}", string.Join(", ", ExtraImageUrls ?? new List<string?>()));
            _logger.LogInformation("ExistingImages count: {Count}", existingImages.Count);

            for (int i = 0; i < 3; i++)
            {
                var newFile = extraFiles[i];
                var currentUrl = (ExtraImageUrls != null && i < ExtraImageUrls.Count)
                    ? (ExtraImageUrls[i] ?? "") : "";
                var existingImg = i < existingImages.Count ? existingImages[i] : null;

                bool hasNewFile = newFile != null && newFile.Length > 0;
                bool isMarker = currentUrl == "new_file";
                bool isEmptyUrl = string.IsNullOrEmpty(currentUrl);

                _logger.LogInformation(
                    "Slot {Slot}: hasNewFile={HasFile} fileLen={Len} isMarker={Marker} isEmptyUrl={Empty} existingImg={Img}",
                    i, hasNewFile, newFile?.Length ?? 0, isMarker, isEmptyUrl, existingImg?.Url ?? "null");

                if (hasNewFile)
                {
                    var err = ValidateImageFile(newFile!);
                    if (err != null)
                    {
                        TempData["Error"] = $"Anh phu {i + 2}: {err}";
                        _logger.LogWarning("Slot {Slot} validation failed: {Err}", i, err);
                        continue;
                    }

                    var newUrl = await SaveImage(newFile!);
                    _logger.LogInformation("Slot {Slot}: saved new file — {Url}", i, newUrl);

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
                else if (isMarker)
                {
                    _logger.LogWarning("Slot {Slot}: isMarker but no file received", i);
                }
                else if (!isEmptyUrl)
                {
                    if (existingImg != null)
                    {
                        if (existingImg.Url != currentUrl)
                        {
                            if (existingImg.Url?.StartsWith("/images/") == true)
                                DeleteOldLocalImage(existingImg.Url);
                            existingImg.Url = currentUrl;
                            _context.ProductImages.Update(existingImg);
                        }
                    }
                    else
                    {
                        _context.ProductImages.Add(new ProductImage { ProductId = id, Url = currentUrl });
                    }
                }
                else
                {
                    if (existingImg != null)
                    {
                        DeleteOldLocalImage(existingImg.Url);
                        _context.ProductImages.Remove(existingImg);
                    }
                }
            }

            _context.Products.Update(existing);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Da cap nhat san pham \"{existing.Name}\" thanh cong!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id) => RedirectToAction(nameof(Edit), new { id });

        // ADMIN — xoa
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
            TempData["Success"] = $"Da xoa san pham \"{name}\" thanh cong!";
            return RedirectToAction(nameof(Index));
        }

        // ========== BAI 6: TRANG DEMO API ==========
        // ADMIN — trang demo RESTful API (fetch/JS goi du 5 CRUD endpoint)
        [Authorize(Roles = "Admin")]
        public IActionResult ApiDemo()
        {
            return View();
        }

        // ── Helpers ──
        private static string? ValidateImageFile(IFormFile file)
        {
            if (file.Length > MaxImageSizeBytes)
                return "Anh khong duoc vuot qua 5MB.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return $"Chi chap nhan anh .jpg, .jpeg, .png, .webp, .gif, .bmp (nhan duoc: '{ext}')";

            return null;
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            var imagesFolder = Path.Combine(webRoot, "images");
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var savePath = Path.Combine(imagesFolder, uniqueFileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
                await image.CopyToAsync(fs);

            _logger.LogInformation("Image file saved to disk: {Path}", savePath);
            return "/images/" + uniqueFileName;
        }

        private void DeleteOldLocalImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/"))
                return;
            try
            {
                var webRoot = _env.WebRootPath;
                if (string.IsNullOrEmpty(webRoot))
                    webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(webRoot, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not delete old image {Url}: {Ex}", imageUrl, ex.Message);
            }
        }
    }
}