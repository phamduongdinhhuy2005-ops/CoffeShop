// Controllers/AccountController.cs
// FIXED:
// Bug 10 — TempData["ActiveTab"] set đúng sau UpdateProfile và ChangePassword
//           → Sau khi save, tab tương ứng mở lại thay vì về mặc định "profile"
// Bug 13 — UserAccount(): bookings chỉ query theo UserId, nhưng MyBookings dùng UserId OR Email
//           → Không nhất quán: booking đặt trước khi có tài khoản sẽ không hiện
//           Fix: dùng (UserId == user.Id || Email == user.Email) giống MyBookings
// Fix encoding: TempData strings dùng UTF-8 đúng chuẩn (không còn mojibake)

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(UserManager<AppUser> userManager,
                                 SignInManager<AppUser> signInManager,
                                 ApplicationDbContext context,
                                 IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }

        private void SetOAuthViewBag()
        {
            ViewBag.GoogleEnabled = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]);
            ViewBag.FacebookEnabled = !string.IsNullOrEmpty(_configuration["Authentication:Facebook:AppId"]);
        }

        // ======== ĐĂNG KÝ ========
        [HttpGet]
        public IActionResult Register()
        {
            SetOAuthViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AccountRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetOAuthViewBag();
                return View(model);
            }

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
                TempData["Success"] = "Đăng ký thành công! Chào mừng bạn đến với Góc Lặng.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            SetOAuthViewBag();
            return View(model);
        }

        // ======== ĐĂNG NHẬP ========
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            SetOAuthViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AccountLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                SetOAuthViewBag();
                return View(model);
            }

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
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.");
                SetOAuthViewBag();
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            SetOAuthViewBag();
            return View(model);
        }

        // ======== ĐĂNG NHẬP NGOÀI (Google / Facebook) ========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp: {remoteError}");
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login");

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Không lấy được email từ tài khoản mạng xã hội.");
                return RedirectToAction("Login");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                await _userManager.AddLoginAsync(existingUser, info);
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "User");
                await _userManager.AddLoginAsync(newUser, info);
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                TempData["Success"] = $"Chào mừng {fullName} đến với Góc Lặng!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return RedirectToAction("Login");
        }

        // ======== ĐĂNG XUẤT ========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ======== ACCESS DENIED ========
        public IActionResult AccessDenied() => View();

        // ======== TÀI KHOẢN ========
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

            // FIX Bug 13: Dùng (UserId == user.Id OR Email == user.Email) nhất quán với MyBookings
            // Booking đặt trước khi tạo tài khoản (UserId = null) vẫn hiện trong tab bookings
            var bookings = await _context.Bookings
                .Where(b => b.UserId == user.Id || b.Email == user.Email)
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
                TempData["Error"] = "Họ tên không được để trống.";
                TempData["ActiveTab"] = "profile";   // Bug 10: giữ đúng tab
                return RedirectToAction("UserAccount");
            }

            user.FullName = fullName.Trim();
            user.PhoneNumber = phoneNumber?.Trim();
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            TempData["ActiveTab"] = "profile";   // Bug 10: mở lại đúng tab
            return RedirectToAction("UserAccount");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // Bug 10: luôn set ActiveTab = "password" cho action này
            TempData["ActiveTab"] = "password";

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["Error"] = "Vui lòng nhập mật khẩu hiện tại.";
                return RedirectToAction("UserAccount");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Vui lòng nhập mật khẩu mới.";
                return RedirectToAction("UserAccount");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("UserAccount");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới không khớp.";
                return RedirectToAction("UserAccount");
            }

            if (currentPassword == newPassword)
            {
                TempData["Error"] = "Mật khẩu mới phải khác mật khẩu hiện tại.";
                return RedirectToAction("UserAccount");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("UserAccount");
        }
    }
}