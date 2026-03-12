using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Booking
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Booking booking)
        {
            if (ModelState.IsValid)
            {
                booking.CreatedAt = DateTime.Now;
                booking.Status = BookingStatus.Pending;
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đặt chỗ thành công! Chúng mình sẽ liên hệ xác nhận với {booking.FullName} qua số {booking.Phone} trong vòng 30 phút.";
                return RedirectToAction("Index");
            }
            return View(booking);
        }
    }
}
