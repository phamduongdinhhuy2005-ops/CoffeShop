// Controllers/CartController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;

namespace WebBanHang_2380600870.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private const string CartSessionKey = "ShoppingCart";

        public CartController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private Cart GetCart()
            => HttpContext.Session.GetObject<Cart>(CartSessionKey) ?? new Cart();

        private void SaveCart(Cart cart)
            => HttpContext.Session.SetObject(CartSessionKey, cart);

        // GET /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string size = "M",
            string sugarLevel = "100%", string toppings = "")
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            decimal price = product.Price;
            // ±7k theo từng bậc kích cỡ (S=-7k, M=giá gốc, L=+7k)
            if (size == "L") price += 7000;
            else if (size == "S") price -= 7000;

            var cart = GetCart();
            cart.AddItem(new CartItem
            {
                ProductId = productId,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl ?? "/images/placeholder.jpg",
                UnitPrice = price,
                Quantity = quantity,
                Size = size,
                SugarLevel = sugarLevel,
                Toppings = toppings
            });
            SaveCart(cart);

            return Json(new
            {
                success = true,
                message = $"Đã thêm {product.Name} vào giỏ hàng!",
                cartCount = cart.TotalQuantity
            });
        }

        // POST /Cart/Remove
        [HttpPost]
        public IActionResult Remove(int productId, string size, string sugarLevel)
        {
            var cart = GetCart();
            cart.RemoveItem(productId, size, sugarLevel);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST /Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, string size, string sugarLevel, int quantity)
        {
            var cart = GetCart();
            cart.UpdateQuantity(productId, size, sugarLevel, quantity);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST /Cart/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            var cart = GetCart();
            cart.Clear();
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // GET /Cart/CartCount (AJAX)
        [HttpGet]
        public IActionResult CartCount()
        {
            var cart = GetCart();
            return Json(new { count = cart.TotalQuantity });
        }

        // GET /Cart/Checkout
        [Authorize]
        public async Task<IActionResult> Checkout()  // FIX: async thay vì sync + .Result
        {
            var cart = GetCart();

            if (cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Menu");
            }

            var user = await _userManager.GetUserAsync(User); // FIX: await thay vì .Result
            var vm = new CheckoutViewModel
            {
                Cart = cart,
                FullName = user?.FullName ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
            };
            return View(vm);
        }

        // POST /Cart/Checkout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            var cart = GetCart();

            if (cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Menu");
            }

            if (!ModelState.IsValid)
            {
                vm.Cart = cart;
                return View(vm);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = cart.TotalAmount,
                Note = vm.Note,
                ShippingAddress = vm.ShippingAddress,
                OrderDetails = cart.Items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            cart.Clear();
            SaveCart(cart);

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng #{order.Id}";
            return RedirectToAction("OrderConfirm", new { orderId = order.Id });
        }

        // GET /Cart/OrderConfirm/{orderId}
        [Authorize]
        public async Task<IActionResult> OrderConfirm(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
