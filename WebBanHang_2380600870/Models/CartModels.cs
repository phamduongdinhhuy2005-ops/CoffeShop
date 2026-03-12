// Models/CartModels.cs
// Thêm vào file Models.cs hoặc tạo file riêng

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

        public void AddItem(CartItem newItem)
        {
            var existing = Items.FirstOrDefault(i =>
                i.ProductId == newItem.ProductId &&
                i.Size == newItem.Size &&
                i.SugarLevel == newItem.SugarLevel &&
                i.Toppings == newItem.Toppings);

            if (existing != null)
                existing.Quantity += newItem.Quantity;
            else
                Items.Add(newItem);
        }

        public void RemoveItem(int productId, string size, string sugarLevel)
        {
            var item = Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.Size == size &&
                i.SugarLevel == sugarLevel);
            if (item != null) Items.Remove(item);
        }

        public void UpdateQuantity(int productId, string size, string sugarLevel, int quantity)
        {
            var item = Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.Size == size &&
                i.SugarLevel == sugarLevel);
            if (item != null)
            {
                if (quantity <= 0) Items.Remove(item);
                else item.Quantity = quantity;
            }
        }

        public void Clear() => Items.Clear();
    }

    // ViewModel cho trang Checkout
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = new();
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string Note { get; set; } = "";
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
