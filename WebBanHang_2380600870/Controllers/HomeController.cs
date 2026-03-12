using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                TempData["SubscribeError"] = "Email không hợp lệ, vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _context.Subscribers.AnyAsync(s => s.Email == email);
            if (exists)
            {
                TempData["SubscribeError"] = "Email này đã đăng ký rồi nhé ☕";
                return RedirectToAction(nameof(Index));
            }

            _context.Subscribers.Add(new Subscriber { Email = email });
            await _context.SaveChangesAsync();

            TempData["SubscribeSuccess"] = $"Cảm ơn! Chúng mình sẽ liên lạc với {email} sớm nhé ☕";
            return RedirectToAction(nameof(Index));
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
