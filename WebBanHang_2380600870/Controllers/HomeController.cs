// Controllers/HomeController.cs
// FIXED:
// Bug 6 — Subscribe: Validate email đúng chuẩn bằng EmailAddress attribute thay vì
//           chỉ check Contains('@') — tránh trường hợp email "a@" hoặc "@b" pass qua.
//           TempData strings dùng UTF-8 đúng (không bị mojibake khi build Release).
//           Thêm trim() trước khi lưu email để tránh duplicate do khoảng trắng.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Repositories;

namespace WebBanHang_2380600870.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public HomeController(IProductRepository productRepository,
                              ICategoryRepository categoryRepository,
                              ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        // GET: /Home/Index
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // GET: /Home/CauChuyen
        public IActionResult CauChuyen()
        {
            return View();
        }

        // POST: /Home/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(string email)
        {
            // FIX Bug 6: Trim trước khi validate để tránh false negative
            email = email?.Trim() ?? "";

            // FIX Bug 6: Validate email đúng chuẩn thay vì chỉ check Contains('@')
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                TempData["SubscribeError"] = "Email không hợp lệ, vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            // FIX Bug 6: So sánh case-insensitive khi check duplicate
            var exists = await _context.Subscribers
                .AnyAsync(s => s.Email.ToLower() == email.ToLower());

            if (exists)
            {
                TempData["SubscribeError"] = "Email này đã đăng ký rồi nhé ☕";
                return RedirectToAction(nameof(Index));
            }

            _context.Subscribers.Add(new Subscriber
            {
                Email = email.ToLower(), // Lưu lowercase để nhất quán
                SubscribedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SubscribeSuccess"] = $"Cảm ơn! Chúng mình sẽ liên lạc với {email} sớm nhé ☕";
            return RedirectToAction(nameof(Index));
        }

        // FIX Bug 6: Helper validate email đúng chuẩn
        private static bool IsValidEmail(string email)
        {
            var attr = new EmailAddressAttribute();
            return attr.IsValid(email);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
    }
}