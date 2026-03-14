// Controllers/AdminController.cs
// FIX CS0168: Bỏ biến 'ex' trong tất cả catch blocks không dùng đến

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Repositories;

namespace WebBanHang_2380600870.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<AppUser> userManager,
                               IProductRepository productRepo,
                               ICategoryRepository categoryRepo,
                               ApplicationDbContext context)
        {
            _userManager = userManager;
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _context = context;
        }

        // ========== DASHBOARD ==========
        public async Task<IActionResult> Index()
        {
            try
            {
                var totalProducts = (await _productRepo.GetAllAsync())?.Count() ?? 0;

                var totalUsers = await _userManager.Users.CountAsync();
                var totalOrders = await _context.Orders.CountAsync();
                var totalBookings = await _context.Bookings.CountAsync();

                ViewBag.TotalProducts = totalProducts;
                ViewBag.TotalUsers = totalUsers;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.TotalBookings = totalBookings;

                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                ViewBag.TotalRevenue = totalRevenue;

                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
                var processingOrders = await _context.Orders.CountAsync(o =>
                    o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Processing);
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.ProcessingOrders = processingOrders;

                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                return View(recentOrders);
            }
            catch (Exception)  // FIX CS0168: bỏ 'ex' — không dùng đến
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải dữ liệu dashboard. Vui lòng thử lại.";
                ViewBag.TotalProducts = 0; ViewBag.TotalUsers = 0;
                ViewBag.TotalOrders = 0; ViewBag.TotalBookings = 0;
                ViewBag.TotalRevenue = 0m;
                ViewBag.PendingOrders = 0;
                ViewBag.ProcessingOrders = 0;
                return View(new List<Order>());
            }
        }

        // ========== QUẢN LÝ TÙY CHỌN SẢN PHẨM THEO CATEGORY ==========
        public async Task<IActionResult> CategoryOptions()
        {
            var categories = await _categoryRepo.GetAllAsync();
            ViewBag.Categories = categories;
            return View();
        }

        // ========== QUẢN LÝ TÀI KHOẢN ==========
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
                var userRoles = new Dictionary<string, IList<string>>();
                foreach (var user in users)
                    userRoles[user.Id] = await _userManager.GetRolesAsync(user);

                ViewBag.UserRoles = userRoles;
                return View(users);
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách người dùng.";
                return View(new List<AppUser>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRole(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Users));
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == userId)
                {
                    TempData["Error"] = "Không thể thay đổi quyền của tài khoản đang đăng nhập.";
                    return RedirectToAction(nameof(Users));
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                    await _userManager.AddToRoleAsync(user, "User");
                    TempData["Success"] = $"Đã hạ quyền {user.FullName ?? user.Email} xuống User.";
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, "User");
                    await _userManager.AddToRoleAsync(user, "Admin");
                    TempData["Success"] = $"Đã nâng quyền {user.FullName ?? user.Email} lên Admin.";
                }

                return RedirectToAction(nameof(Users));
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi thay đổi quyền người dùng.";
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Users));
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == userId)
                {
                    TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập.";
                    return RedirectToAction(nameof(Users));
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    TempData["Error"] = "Không thể xóa tài khoản Admin. Hãy hạ quyền trước.";
                    return RedirectToAction(nameof(Users));
                }

                var result = await _userManager.DeleteAsync(user);
                TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                    ? "Đã xóa tài khoản thành công."
                    : "Không thể xóa tài khoản. Vui lòng thử lại.";

                return RedirectToAction(nameof(Users));
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa người dùng.";
                return RedirectToAction(nameof(Users));
            }
        }

        // ========== QUẢN LÝ ĐƠN HÀNG ==========
        public async Task<IActionResult> Orders(string? status = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
                    query = query.Where(o => o.Status == s);

                var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
                ViewBag.CurrentStatus = status;

                ViewBag.StatusCounts = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

                return View(orders);
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đơn hàng.";
                return View(new List<Order>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(Orders));
                }

                if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                {
                    TempData["Error"] = "Không thể thay đổi trạng thái đơn hàng đã hoàn thành hoặc đã hủy.";
                    return RedirectToAction(nameof(Orders));
                }

                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật đơn #{orderId} sang trạng thái: {status}.";
                return RedirectToAction(nameof(Orders));
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng.";
                return RedirectToAction(nameof(Orders));
            }
        }

        // ========== QUẢN LÝ ĐẶT CHỖ ==========
        public async Task<IActionResult> Bookings(string? status = null)
        {
            try
            {
                var query = _context.Bookings.AsQueryable();

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var s))
                    query = query.Where(b => b.Status == s);

                var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
                ViewBag.CurrentStatus = status;
                return View(bookings);
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đặt chỗ.";
                return View(new List<Booking>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, BookingStatus status)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    TempData["Error"] = "Không tìm thấy đặt chỗ.";
                    return RedirectToAction(nameof(Bookings));
                }

                booking.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật trạng thái đặt chỗ của {booking.FullName}.";
                return RedirectToAction(nameof(Bookings));
            }
            catch (Exception)  // FIX CS0168
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái đặt chỗ.";
                return RedirectToAction(nameof(Bookings));
            }
        }
    }
}