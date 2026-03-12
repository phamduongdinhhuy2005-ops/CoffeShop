// Models/ProductDetailViewModel.cs
// ✅ FIX: File này phải nằm trong thư mục Models/, KHÔNG phải Controllers/
// Xóa file Controllers/ProductDetailViewModel.cs nếu tồn tại

namespace WebBanHang_2380600870.Models
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();
    }
}
