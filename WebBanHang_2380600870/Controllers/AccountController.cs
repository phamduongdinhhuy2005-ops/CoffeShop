// Controllers/AccountController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<AppUser> userManager,
                                 SignInManager<AppUser> signInManager,
                                 ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // ======== ÄÄ‚NG KÃ ========
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AccountRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = "ÄÄƒng kÃ½ thÃ nh cÃ´ng! ChÃ o má»«ng báº¡n Ä‘áº¿n vá»›i GÃ³c Láº·ng.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // ======== ÄÄ‚NG NHáº¬P ========
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AccountLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Admin");
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "TÃ i khoáº£n Ä‘Ã£ bá»‹ khÃ³a do Ä‘Äƒng nháº­p sai quÃ¡ nhiá»u láº§n. Vui lÃ²ng thá»­ láº¡i sau 15 phÃºt.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Email hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng.");
            return View(model);
        }

        // ======== ÄÄ‚NG XUáº¤T ========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ======== ACCESS DENIED ========
        public IActionResult AccessDenied() => View();

        // ======== TÃ€I KHOáº¢N ========
        [Authorize]
        public async Task<IActionResult> UserAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // FIX: Load bookings của user để hiện trong tab Lịch Đặt Chỗ
            var bookings = await _context.Bookings
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.Orders = orders;
            ViewBag.Bookings = bookings;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Há» tÃªn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.";
                return RedirectToAction("UserAccount");
            }

            user.FullName = fullName.Trim();
            user.PhoneNumber = phoneNumber?.Trim();
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Cáº­p nháº­t thÃ´ng tin thÃ nh cÃ´ng!";
            return RedirectToAction("UserAccount");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["Error"] = "Vui lÃ²ng nháº­p máº­t kháº©u hiá»‡n táº¡i.";
                return RedirectToAction("UserAccount");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Vui lÃ²ng nháº­p máº­t kháº©u má»›i.";
                return RedirectToAction("UserAccount");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Máº­t kháº©u má»›i pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.";
                return RedirectToAction("UserAccount");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Máº­t kháº©u má»›i khÃ´ng khá»›p.";
                return RedirectToAction("UserAccount");
            }

            if (currentPassword == newPassword)
            {
                TempData["Error"] = "Máº­t kháº©u má»›i pháº£i khÃ¡c máº­t kháº©u hiá»‡n táº¡i.";
                return RedirectToAction("UserAccount");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
                TempData["Success"] = "Äá»•i máº­t kháº©u thÃ nh cÃ´ng!";
            else
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction("UserAccount");
        }
    }
}
