// FILE: Controllers/StoryController.cs
using Microsoft.AspNetCore.Mvc;

namespace WebBanHang_2380600870.Controllers
{
    public class StoryController : Controller
    {
        /// <summary>
        /// Hiển thị trang "Câu Chuyện Của Chúng Mình"
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }
    }
}
