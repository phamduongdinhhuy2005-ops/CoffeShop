// Controllers/AdminController.cs
// FIXED:
// Bug 3 — DeleteUser: Thêm check có Orders đang xử lý trước khi xóa, tránh FK violation
//           User vẫn có thể bị xóa khi chỉ có orders Completed/Cancelled (EF Cascade xử lý)
// Bug 4 — UpdateOrderStatus: Bổ sung check không cho update sang Cancelled bằng dropdown
//           nếu muốn cancel phải dùng flow riêng; fix logic guard cho Completed/Cancelled
// Bug 5 — Bookings GET: Giữ filter status qua ViewBag đúng cách, không reset khi có lỗi
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
            catch (Exception)
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
            catch (Exception)
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
            catch (Exception)
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

                // FIX Bug 3: Kiểm tra các đơn hàng đang xử lý (Pending/Confirmed/Processing/Ready)
                // Nếu còn đơn đang active thì không cho xóa, tránh mất dữ liệu quan trọng
                // EF Cascade sẽ xử lý xóa Orders Completed/Cancelled tự động
                var activeOrderCount = await _context.Orders.CountAsync(o =>
                    o.UserId == user.Id &&
                    o.Status != OrderStatus.Completed &&
                    o.Status != OrderStatus.Cancelled);

                if (activeOrderCount > 0)
                {
                    TempData["Error"] = $"Không thể xóa tài khoản này vì còn {activeOrderCount} đơn hàng đang xử lý. Hãy hoàn thành hoặc hủy các đơn trước.";
                    return RedirectToAction(nameof(Users));
                }

                var result = await _userManager.DeleteAsync(user);
                TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                    ? "Đã xóa tài khoản thành công."
                    : "Không thể xóa tài khoản. Vui lòng thử lại.";

                return RedirectToAction(nameof(Users));
            }
            catch (Exception)
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
            catch (Exception)
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

                // FIX Bug 4: Phân biệt rõ 2 trường hợp:
                // - Đã Completed: không cho phép thay đổi bất kỳ trạng thái nào
                // - Đã Cancelled: không cho phép thay đổi (đơn đã hủy là cuối cùng)
                if (order.Status == OrderStatus.Completed)
                {
                    TempData["Error"] = "Không thể thay đổi trạng thái đơn hàng đã hoàn thành.";
                    return RedirectToAction(nameof(Orders));
                }

                if (order.Status == OrderStatus.Cancelled)
                {
                    TempData["Error"] = "Không thể thay đổi trạng thái đơn hàng đã hủy.";
                    return RedirectToAction(nameof(Orders));
                }

                // FIX Bug 4: Admin không được dùng dropdown này để cancel đơn hàng
                // (Cancelled là action đặc biệt, chỉ user mới cancel được đơn Pending của mình)
                // Admin có thể update mọi status trừ Cancelled
                if (status == OrderStatus.Cancelled)
                {
                    TempData["Error"] = "Admin không thể hủy đơn hàng qua trang này. Vui lòng liên hệ khách hàng để phối hợp.";
                    return RedirectToAction(nameof(Orders));
                }

                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật đơn #{orderId} sang trạng thái: {status}.";
                return RedirectToAction(nameof(Orders));
            }
            catch (Exception)
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

                // FIX Bug 5: Giữ lại CurrentStatus trong ViewBag để view filter đúng
                ViewBag.CurrentStatus = status;

                return View(bookings);
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đặt chỗ.";
                // FIX Bug 5: Khi có lỗi vẫn pass ViewBag.CurrentStatus để tránh null ref trong view
                ViewBag.CurrentStatus = status;
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
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái đặt chỗ.";
                return RedirectToAction(nameof(Bookings));
            }
        }
    }
}