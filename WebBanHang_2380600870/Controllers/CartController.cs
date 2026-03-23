// Controllers/CartController.cs
// FIX IDENTITY_INSERT: Dùng ExecutionStrategy.ExecuteAsync để tắt retry logic của SQL Server.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Services;

namespace WebBanHang_2380600870.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IdGeneratorService _idGen;
        private const string CartSessionKey = "ShoppingCart";

        public CartController(ApplicationDbContext context,
                               UserManager<AppUser> userManager,
                               IdGeneratorService idGen)
        {
            _context = context;
            _userManager = userManager;
            _idGen = idGen;
        }

        private Cart GetCart() => HttpContext.Session.GetObject<Cart>(CartSessionKey) ?? new Cart();
        private void SaveCart(Cart cart) => HttpContext.Session.SetObject(CartSessionKey, cart);

        public IActionResult Index()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            return View(GetCart());
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string size = "M",
            string sugarLevel = "100%", string toppings = "", decimal toppingPrice = 0)
        {
            if (User.IsInRole("Admin"))
                return Json(new { success = false, message = "Tài khoản Admin không thể thêm vào giỏ hàng." });

            if (quantity < 1) quantity = 1;
            if (quantity > 99) quantity = 99;

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            decimal price = product.Price;
            if (product.IsOnSale && product.DiscountPercent.HasValue && product.DiscountPercent > 0)
                price = product.SalePrice;

            if (size == "L") price += 7000;
            else if (size == "S") price = Math.Max(0, price - 7000);

            if (toppingPrice > 0) price += toppingPrice;

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
                Toppings = toppings ?? ""
            });
            SaveCart(cart);

            return Json(new { success = true, message = $"Đã thêm {product.Name} vào giỏ hàng!", cartCount = cart.TotalQuantity });
        }

        [HttpPost]
        public IActionResult Remove(int productId, string size, string sugarLevel, string toppings = "")
        {
            var cart = GetCart();
            cart.RemoveItem(productId, size, sugarLevel, toppings);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, string size, string sugarLevel, int quantity, string toppings = "")
        {
            if (quantity < 0) quantity = 0;
            if (quantity > 99) quantity = 99;
            var cart = GetCart();
            cart.UpdateQuantity(productId, size, sugarLevel, quantity, toppings);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            var cart = GetCart();
            cart.Clear();
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CartCount()
        {
            if (User.IsInRole("Admin")) return Json(new { count = 0 });
            return Json(new { count = GetCart().TotalQuantity });
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            var cart = GetCart();
            if (cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống. Hãy thêm sản phẩm từ thực đơn.";
                return RedirectToAction("Index", "Menu");
            }
            var user = await _userManager.GetUserAsync(User);
            return View(new CheckoutViewModel
            {
                Cart = cart,
                FullName = user?.FullName ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            var cart = GetCart();
            if (cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống. Hãy thêm sản phẩm từ thực đơn.";
                return RedirectToAction("Index", "Menu");
            }

            ModelState.Remove("Cart");
            if (!ModelState.IsValid) { vm.Cart = cart; return View(vm); }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) { TempData["Error"] = "Vui lòng đăng nhập."; return RedirectToAction("Login", "Account"); }

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalAmount = cart.TotalAmount,
                Note = vm.Note?.Trim(),
                ShippingAddress = vm.ShippingAddress?.Trim(),
                PaymentMethod = vm.PaymentMethod ?? "COD",
                OrderDetails = cart.Items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    ItemNote = $"Size: {i.Size} | Đường: {i.SugarLevel}" +
                                (string.IsNullOrEmpty(i.Toppings) ? "" : $" | Topping: {i.Toppings}")
                }).ToList()
            };

            order.Id = await _idGen.NextOrderIdAsync();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Orders ON");
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Orders OFF");
                await transaction.CommitAsync();
            });

            await _idGen.ReseedOrdersAsync();
            cart.Clear(); SaveCart(cart);

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng #{order.Id}";
            return RedirectToAction("OrderConfirm", new { orderId = order.Id });
        }

        [Authorize]
        public async Task<IActionResult> OrderConfirm(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var order = await _context.Orders
                .Include(o => o.OrderDetails!).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);
            if (order == null) return NotFound();
            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Orders", "Admin");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var orders = await _context.Orders
                .Include(o => o.OrderDetails!).ThenInclude(od => od.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewBag.CustomerName = user.FullName ?? user.Email ?? "";
            ViewBag.CustomerPhone = user.PhoneNumber ?? "";
            return View(orders);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderHistory));
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xác nhận.";
                return RedirectToAction(nameof(OrderHistory));
            }

            order.Status = OrderStatus.Cancelled;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã hủy đơn hàng #{order.Id} thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Đơn hàng đã được cập nhật trước đó. Vui lòng kiểm tra lại trạng thái.";
            }

            return RedirectToAction(nameof(OrderHistory));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderHistory));
            }

            if (order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Completed)
            {
                TempData["Error"] = "Chỉ có thể xóa đơn hàng đã hoàn thành hoặc đã hủy.";
                return RedirectToAction(nameof(OrderHistory));
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa đơn hàng #{orderId} khỏi lịch sử.";
            return RedirectToAction(nameof(OrderHistory));
        }
    }
}