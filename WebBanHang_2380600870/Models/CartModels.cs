// Models/CartModels.cs
// FIXED & OPTIMIZED:
// 1. RemoveItem() cũ không match đúng khi Toppings khác nhau → mất item sai
// 2. UpdateQuantity() thiếu toppings trong key → update nhầm item
// 3. CheckoutViewModel thiếu [Required] cho ShippingAddress → có thể đặt hàng không địa chỉ
// 4. Thêm MaxQuantity validation trong Cart.AddItem

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WebBanHang_2380600870.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string Size { get; set; } = "M"; // S, M, L
        public string SugarLevel { get; set; } = "100%";
        public string Toppings { get; set; } = "";
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(i => i.LineTotal);
        public int TotalQuantity => Items.Sum(i => i.Quantity);

        // FIX: Key matching phải bao gồm Toppings để tránh merge nhầm item
        private static bool IsSameItem(CartItem a, CartItem b)
            => a.ProductId == b.ProductId
            && a.Size == b.Size
            && a.SugarLevel == b.SugarLevel
            && a.Toppings == b.Toppings;

        public void AddItem(CartItem newItem)
        {
            var existing = Items.FirstOrDefault(i => IsSameItem(i, newItem));

            if (existing != null)
            {
                // FIX: Giới hạn tối đa 99 sản phẩm/item
                existing.Quantity = Math.Min(99, existing.Quantity + newItem.Quantity);
            }
            else
            {
                Items.Add(newItem);
            }
        }

        // FIX: Thêm toppings vào signature để match đúng item
        public void RemoveItem(int productId, string size, string sugarLevel, string toppings = "")
        {
            var item = Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.Size == size &&
                i.SugarLevel == sugarLevel &&
                i.Toppings == (toppings ?? ""));
            if (item != null) Items.Remove(item);
        }

        // FIX: Thêm toppings vào signature để match đúng item
        public void UpdateQuantity(int productId, string size, string sugarLevel, int quantity, string toppings = "")
        {
            var item = Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.Size == size &&
                i.SugarLevel == sugarLevel &&
                i.Toppings == (toppings ?? ""));
            if (item != null)
            {
                if (quantity <= 0) Items.Remove(item);
                else item.Quantity = Math.Min(99, quantity);
            }
        }

        public void Clear() => Items.Clear();
    }

    // FIX: Thêm validation attributes bị thiếu
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = new();

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = "";

        // FIX: Thêm [Required] - trước đây có thể đặt hàng mà không điền địa chỉ
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [StringLength(500, ErrorMessage = "Địa chỉ không quá 500 ký tự")]
        public string ShippingAddress { get; set; } = "";

        [StringLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
        public string? Note { get; set; }

        public string PaymentMethod { get; set; } = "COD"; // COD hoặc Transfer
    }

    // Session helper
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
            => session.SetString(key, JsonSerializer.Serialize(value));

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
