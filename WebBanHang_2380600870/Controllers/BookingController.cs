// Controllers/BookingController.cs
// FIX IDENTITY_INSERT: Dùng ExecutionStrategy.ExecuteAsync để tắt retry logic của SQL Server.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Services;

namespace WebBanHang_2380600870.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IdGeneratorService _idGen;

        public BookingController(ApplicationDbContext context,
                                 UserManager<AppUser> userManager,
                                 IdGeneratorService idGen)
        {
            _context = context;
            _userManager = userManager;
            _idGen = idGen;
        }

        // GET: /Booking
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admin vui lòng quản lý đặt chỗ tại trang Admin.";
                return RedirectToAction("Bookings", "Admin");
            }

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
            if (User.IsInRole("Admin"))
                return RedirectToAction("Bookings", "Admin");

            if (booking.BookingDate.Date < DateTime.Today)
                ModelState.AddModelError("BookingDate", "Ngày đặt chỗ không được là ngày trong quá khứ.");

            if (booking.BookingDate.Date > DateTime.Today.AddDays(30))
                ModelState.AddModelError("BookingDate", "Chỉ có thể đặt chỗ trong vòng 30 ngày tới.");

            if (!string.IsNullOrEmpty(booking.BookingTime))
            {
                var validTimes = new HashSet<string> {
                    "07:00","07:30","08:00","08:30","09:00","09:30",
                    "10:00","10:30","11:00","11:30","12:00","12:30",
                    "13:00","13:30","14:00","14:30","15:00","15:30",
                    "16:00","16:30","17:00","17:30","18:00","18:30",
                    "19:00","19:30","20:00","20:30","21:00"
                };
                if (!validTimes.Contains(booking.BookingTime))
                    ModelState.AddModelError("BookingTime", "Giờ đặt chỗ không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                booking.CreatedAt = DateTime.UtcNow;
                booking.Status = BookingStatus.Pending;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    booking.UserId = user?.Id;
                }

                booking.Id = await _idGen.NextBookingIdAsync();

                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Bookings ON");
                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Bookings OFF");
                    await transaction.CommitAsync();
                });

                await _idGen.ReseedBookingsAsync();

                TempData["Success"] = $"Đặt chỗ thành công! Chúng mình sẽ liên hệ xác nhận với {booking.FullName} qua số {booking.Phone} trong vòng 30 phút.";
                return RedirectToAction("Index");
            }
            return View(booking);
        }

        // GET: /Booking/MyBookings
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Bookings", "Admin");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Where(b => b.UserId == user.Id || b.Email == user.Email)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

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