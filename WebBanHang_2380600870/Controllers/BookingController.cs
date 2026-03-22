// Controllers/BookingController.cs
// UPDATED:
// 1. Thêm MyBookings action - lịch sử đặt chỗ của user đăng nhập
// 2. Admin redirect về Admin/Bookings khi vào /Booking
// 3. Validate ngày đặt không trong quá khứ + quá 30 ngày
// 4. Pre-fill thông tin user đã đăng nhập vào form Booking
// 5. Lưu UserId khi đặt chỗ nếu đã đăng nhập

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Booking
        public async Task<IActionResult> Index()
        {
            // FIX: Admin redirect về trang quản lý booking
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin vui lòng quản lý đặt chỗ tại trang Admin.";
                return RedirectToAction("Bookings", "Admin");
            }

            // Pre-fill thông tin nếu đã đăng nhập
            var vm = new Booking();
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    vm.FullName = user.FullName ?? "";
                    vm.Phone = user.PhoneNumber ?? "";
                    vm.Email = user.Email ?? "";
                }
            }

            return View(vm);
        }

        // POST: /Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Booking booking)
        {
            // Block Admin
            if (User.IsInRole("Admin"))
                return RedirectToAction("Bookings", "Admin");

            // Validate ngày không được trong quá khứ
            if (booking.BookingDate.Date < DateTime.Today)
                ModelState.AddModelError("BookingDate", "Ngày đặt chỗ không được là ngày trong quá khứ.");

            // Validate ngày không quá xa (30 ngày)
            if (booking.BookingDate.Date > DateTime.Today.AddDays(30))
                ModelState.AddModelError("BookingDate", "Chỉ có thể đặt chỗ trong vòng 30 ngày tới.");

            // Validate giờ đặt
            if (!string.IsNullOrEmpty(booking.BookingTime))
            {
                var validTimes = new HashSet<string> {
                    "07:00", "07:30", "08:00", "08:30", "09:00", "09:30",
                    "10:00", "10:30", "11:00", "11:30", "12:00", "12:30",
                    "13:00", "13:30", "14:00", "14:30", "15:00", "15:30",
                    "16:00", "16:30", "17:00", "17:30", "18:00", "18:30",
                    "19:00", "19:30", "20:00", "20:30", "21:00"
                };
                if (!validTimes.Contains(booking.BookingTime))
                    ModelState.AddModelError("BookingTime", "Giờ đặt chỗ không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                booking.CreatedAt = DateTime.UtcNow;
                booking.Status = BookingStatus.Pending;

                // Gắn UserId nếu đã đăng nhập
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    booking.UserId = user?.Id;
                }

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đặt chỗ thành công! Chúng mình sẽ liên hệ xác nhận với {booking.FullName} qua số {booking.Phone} trong vòng 30 phút.";
                return RedirectToAction("Index");
            }
            return View(booking);
        }

        // GET: /Booking/MyBookings
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            // Admin không dùng chức năng này
            if (User.IsInRole("Admin"))
                return RedirectToAction("Bookings", "Admin");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Lấy bookings theo UserId hoặc email (cover cả booking trước khi login)
            var bookings = await _context.Bookings
                .Where(b => b.UserId == user.Id || b.Email == user.Email)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // DELETE: User xóa lịch đặt chỗ khỏi lịch sử
        // Điều kiện: không thể xóa khi status = Confirmed (đã xác nhận bởi quán)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(int bookingId)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Bookings", "Admin");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId &&
                    (b.UserId == user.Id || b.Email == user.Email));

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy lịch đặt chỗ.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.Status == BookingStatus.Confirmed)
            {
                TempData["Error"] = "Không thể xóa lịch đặt chỗ đã được xác nhận. Vui lòng liên hệ quán để hủy trước.";
                return RedirectToAction(nameof(MyBookings));
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa lịch đặt chỗ ngày {booking.BookingDate:dd/MM/yyyy} lúc {booking.BookingTime}.";
            return RedirectToAction(nameof(MyBookings));
        }
    }
}
