// FILE: Controllers/GiftCardController.cs
using Microsoft.AspNetCore.Mvc;

namespace WebBanHang_2380600870.Controllers
{
    public class GiftCardController : Controller
    {
        /// <summary>
        /// Hiển thị trang Gift Card
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Xử lý mua Gift Card
        /// </summary>
        [HttpPost]
        public IActionResult Purchase(int amount)
        {
            // TODO: Implement gift card purchase logic
            TempData["Success"] = $"Đã thêm Gift Card {amount:N0}đ vào giỏ hàng!";
            return RedirectToAction("Index");
        }
    }
}
