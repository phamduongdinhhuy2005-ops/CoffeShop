// Controllers/ProductApiController.cs
// BAI 6 — RESTful API
// Browser -> HTML viewer dep, API client -> JSON binh thuong

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Repositories;

namespace WebBanHang_2380600870.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Produces("application/json")]
    public class ProductApiController : ControllerBase
    {
        private readonly IProductRepository _productRepo;
        private readonly ApplicationDbContext _context;

        public ProductApiController(IProductRepository productRepo, ApplicationDbContext context)
        {
            _productRepo = productRepo;
            _context = context;
        }

        // ========== GET /api/products ==========
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? onSaleOnly = null)
        {
            try
            {
                var products = await _productRepo.GetAllAsync();

                if (categoryId.HasValue)
                    products = products.Where(p => p.CategoryId == categoryId.Value);
                if (onSaleOnly == true)
                    products = products.Where(p => p.IsOnSale);

                var list = products.Select(p => new ProductItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    IsOnSale = p.IsOnSale,
                    DiscountPercent = p.DiscountPercent,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    ImageCount = p.Images != null ? p.Images.Count() : 0
                }).ToList();

                // Detect browser
                var accept = Request.Headers["Accept"].ToString();
                bool isBrowser = accept.Contains("text/html");

                if (isBrowser)
                {
                    var html = HtmlViewer.Build(list);
                    return Content(html, "text/html", Encoding.UTF8);
                }

                // JSON response
                Response.Headers.Append("X-Total-Count", list.Count.ToString());
                Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");
                return Ok(list.Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    salePrice = p.SalePrice,
                    p.IsOnSale,
                    p.DiscountPercent,
                    p.Description,
                    p.ImageUrl,
                    p.CategoryId,
                    categoryName = p.CategoryName,
                    imageCount = p.ImageCount
                }));
            }
            catch (Exception ex)
            {
                return Problem(title: "Loi server.", detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // ========== GET /api/products/categories ==========
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var cats = await _context.Categories
                    .OrderBy(c => c.Id)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();
                return Ok(cats);
            }
            catch (Exception ex)
            {
                return Problem(title: "Loi server.", detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // ========== GET /api/products/{id} ==========
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var p = await _productRepo.GetByIdAsync(id);
                if (p == null)
                    return NotFound(new { message = $"Khong tim thay san pham ID = {id}." });

                return Ok(new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    SalePrice = p.SalePrice,
                    p.IsOnSale,
                    p.DiscountPercent,
                    p.Description,
                    p.ImageUrl,
                    p.CategoryId,
                    CategoryName = p.Category?.Name,
                    Images = p.Images?.Select(img => new { img.Id, img.Url })
                                      ?? Enumerable.Empty<object>()
                });
            }
            catch (Exception ex)
            {
                return Problem(title: "Loi server.", detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // ========== POST /api/products ==========
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody] ProductApiDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                if (!await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId))
                {
                    ModelState.AddModelError("CategoryId", $"Danh muc ID = {dto.CategoryId} khong ton tai.");
                    return UnprocessableEntity(ModelState);
                }

                var product = new Product
                {
                    Name = dto.Name.Trim(),
                    Price = dto.Price,
                    Description = dto.Description?.Trim(),
                    ImageUrl = dto.ImageUrl?.Trim(),
                    CategoryId = dto.CategoryId,
                    IsOnSale = dto.IsOnSale,
                    DiscountPercent = dto.DiscountPercent
                };

                await _productRepo.AddAsync(product);
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id },
                    new
                    {
                        product.Id,
                        product.Name,
                        product.Price,
                        product.CategoryId,
                        message = "Tao san pham thanh cong."
                    });
            }
            catch (Exception ex)
            {
                return Problem(title: "Loi server.", detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // ========== PUT /api/products/{id} ==========
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductApiDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var existing = await _productRepo.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Khong tim thay san pham ID = {id}." });

                if (!await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId))
                {
                    ModelState.AddModelError("CategoryId", $"Danh muc ID = {dto.CategoryId} khong ton tai.");
                    return UnprocessableEntity(ModelState);
                }

                existing.Name = dto.Name.Trim();
                existing.Price = dto.Price;
                existing.Description = dto.Description?.Trim();
                existing.ImageUrl = dto.ImageUrl?.Trim();
                existing.CategoryId = dto.CategoryId;
                existing.IsOnSale = dto.IsOnSale;
                existing.DiscountPercent = dto.DiscountPercent;

                await _productRepo.UpdateAsync(existing);
                return NoContent();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                return Conflict(new { message = $"San pham ID = {id} da bi thay doi boi request khac." });
            }
            catch (Exception ex)
            {
                return Problem(title: "Loi server.", detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // ========== DELETE /api/products/{id} ==========
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var existing = await _productRepo.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Khong tim thay san pham ID = {id}." });

                if (await _context.OrderDetails.AnyAsync(od => od.ProductId == id))
                    return Conflict(new { message = $"Khong the xoa san pham ID = {id} vi co don hang lien ket." });

                await _productRepo.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(title: "Loi server.", detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }

    // ── DTO ─────────────────────────────────────────────────────
    public class ProductApiDto
    {
        [Required(ErrorMessage = "Ten san pham khong duoc de trong")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        [Required]
        [Range(100, 10_000_000)]
        public decimal Price { get; set; }

        [StringLength(1000)] public string? Description { get; set; }
        [StringLength(2048)] public string? ImageUrl { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        public bool IsOnSale { get; set; } = false;

        [Range(0, 100)] public decimal? DiscountPercent { get; set; }
    }

    // ── Internal model ───────────────────────────────────────────
    internal class ProductItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsOnSale { get; set; }
        public decimal? DiscountPercent { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int ImageCount { get; set; }
    }

    // ── HTML Viewer ──────────────────────────────────────────────
    // Dung StringBuilder thuan, KHONG dung $@ interpolation de tranh loi voi JS
    internal static class HtmlViewer
    {
        // HTML encode
        private static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        // JSON string escape
        private static string J(string? s) => (s ?? "")
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        public static string Build(List<ProductItem> products)
        {
            // 1) Build JS data array string
            var dataSb = new StringBuilder("[");
            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                dataSb.Append("{");
                dataSb.Append("\"id\":").Append(p.Id).Append(",");
                dataSb.Append("\"name\":\"").Append(J(p.Name)).Append("\",");
                dataSb.Append("\"price\":").Append(p.Price).Append(",");
                dataSb.Append("\"salePrice\":").Append(p.SalePrice).Append(",");
                dataSb.Append("\"isOnSale\":").Append(p.IsOnSale ? "true" : "false").Append(",");
                dataSb.Append("\"discountPercent\":");
                if (p.DiscountPercent.HasValue) dataSb.Append(p.DiscountPercent.Value);
                else dataSb.Append("null");
                dataSb.Append(",");
                dataSb.Append("\"description\":\"").Append(J(p.Description)).Append("\",");
                dataSb.Append("\"imageUrl\":\"").Append(J(p.ImageUrl)).Append("\",");
                dataSb.Append("\"categoryId\":").Append(p.CategoryId).Append(",");
                dataSb.Append("\"categoryName\":\"").Append(J(p.CategoryName)).Append("\",");
                dataSb.Append("\"imageCount\":").Append(p.ImageCount);
                dataSb.Append("}");
                if (i < products.Count - 1) dataSb.Append(",");
            }
            dataSb.Append("]");

            // 2) CSS string (khong co JS, an toan voi StringBuilder)
            var css = GetCss();

            // 3) JS string (TACH BIET, khong dung C# interpolation)
            var js = GetJs(dataSb.ToString(), products.Count);

            // 4) HTML skeleton - chi dung string.Concat, khong $@
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"vi\">\n<head>\n");
            sb.Append("<meta charset=\"UTF-8\"/>\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1.0\"/>\n");
            sb.Append("<title>Goc Lang \u2014 /api/products</title>\n");
            sb.Append("<link href=\"https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,700;0,900;1,400&family=DM+Sans:wght@300;400;500;600&display=swap\" rel=\"stylesheet\"/>\n");
            sb.Append("<style>\n").Append(css).Append("\n</style>\n");
            sb.Append("</head>\n<body>\n");

            // Header
            sb.Append("<header class=\"hdr\">\n");
            sb.Append("  <div class=\"hdr-l\">\n");
            sb.Append("    <div class=\"hdr-ico\">\u2615</div>\n");
            sb.Append("    <div>\n");
            sb.Append("      <div class=\"hdr-title\">G\u00f3c L\u1eb7ng</div>\n");
            sb.Append("      <div class=\"hdr-sub\">API \u2014 /api/products</div>\n");
            sb.Append("    </div>\n");
            sb.Append("  </div>\n");
            sb.Append("  <div class=\"hdr-badge\" id=\"cnt\"></div>\n");
            sb.Append("</header>\n");

            // Controls
            sb.Append("<div class=\"ctrl\">\n");
            sb.Append("  <input class=\"inp\" id=\"q\" value=\"\" autocomplete=\"off\" placeholder=\"\ud83d\udd0d T\u00ecm s\u1ea3n ph\u1ea9m...\" oninput=\"fil()\"/>\n");
            sb.Append("  <select class=\"sel\" id=\"cat\" onchange=\"fil()\"><option value=\"\">T\u1ea5t c\u1ea3</option></select>\n");
            sb.Append("  <select class=\"sel\" id=\"srt\" onchange=\"fil()\">\n");
            sb.Append("    <option value=\"\">M\u1eb7c \u0111\u1ecbnh</option>\n");
            sb.Append("    <option value=\"pa\">Gi\u00e1 t\u0103ng</option>\n");
            sb.Append("    <option value=\"pd\">Gi\u00e1 gi\u1ea3m</option>\n");
            sb.Append("    <option value=\"na\">T\u00ean A\u2192Z</option>\n");
            sb.Append("  </select>\n");
            sb.Append("  <label class=\"chk\"><input type=\"checkbox\" id=\"sale\" onchange=\"fil()\"/> Ch\u1ec9 sale</label>\n");
            sb.Append("</div>\n");

            // Grid
            sb.Append("<div class=\"grid\" id=\"grid\"></div>\n");

            // Modal
            sb.Append("<div class=\"ov\" id=\"ov\" onclick=\"if(event.target===this)cm()\">\n");
            sb.Append("  <div class=\"modal\">\n");
            sb.Append("    <button class=\"mclose\" onclick=\"cm()\">\u00d7</button>\n");
            sb.Append("    <div class=\"mi-w\" id=\"mi\"></div>\n");
            sb.Append("    <div style=\"padding:1.5rem\">\n");
            sb.Append("      <div class=\"mcat\" id=\"mc\"></div>\n");
            sb.Append("      <div class=\"mname\" id=\"mn\"></div>\n");
            sb.Append("      <div class=\"mprice\" id=\"mp\"></div>\n");
            sb.Append("      <div class=\"mdesc\" id=\"md\"></div>\n");
            sb.Append("      <div class=\"mmeta\" id=\"mm\"></div>\n");
            sb.Append("      <button class=\"jsonbtn\" onclick=\"tj()\">&#x7B;&#x7D; Xem JSON</button>\n");
            sb.Append("      <pre class=\"jsonbox\" id=\"jb\"></pre>\n");
            sb.Append("    </div>\n");
            sb.Append("  </div>\n");
            sb.Append("</div>\n");

            // Script
            sb.Append("<script>\n").Append(js).Append("\n</script>\n");
            sb.Append("</body>\n</html>");

            return sb.ToString();
        }

        private static string GetCss()
        {
            // CSS thuan, khong co JS, an toan 100%
            return @"
:root{--cr:#F5EFE6;--br:#A27B5C;--dk:#1a1512;--bd:rgba(162,123,92,.18);--sl:#e05c4b}
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'DM Sans',sans-serif;background:var(--dk);color:var(--cr);min-height:100vh}
.hdr{background:linear-gradient(135deg,#1a1512,#2c2018 50%,#1a1512);border-bottom:1px solid var(--bd);
  padding:1.25rem 2rem;display:flex;align-items:center;justify-content:space-between;
  position:sticky;top:0;z-index:100}
.hdr-l{display:flex;align-items:center;gap:.75rem}
.hdr-ico{width:40px;height:40px;background:var(--br);border-radius:10px;display:flex;
  align-items:center;justify-content:center;font-size:1.25rem;flex-shrink:0}
.hdr-title{font-family:'Playfair Display',serif;font-size:1.25rem;font-weight:700}
.hdr-sub{font-size:.62rem;color:var(--br);letter-spacing:.16em;text-transform:uppercase;margin-top:2px}
.hdr-badge{background:rgba(162,123,92,.15);border:1px solid rgba(162,123,92,.3);color:var(--br);
  font-size:.68rem;font-weight:600;padding:.28rem .8rem;border-radius:99px}
.ctrl{padding:1rem 2rem;display:flex;align-items:center;gap:.625rem;flex-wrap:wrap;
  border-bottom:1px solid rgba(255,255,255,.05);background:rgba(0,0,0,.1)}
.inp{flex:1;min-width:200px;padding:.5rem .875rem;background:rgba(255,255,255,.07);
  border:1px solid rgba(162,123,92,.25);border-radius:8px;color:var(--cr);font-size:.85rem;
  outline:none;font-family:inherit}
.inp:focus{border-color:var(--br)}
.sel{padding:.5rem .75rem;background:rgba(255,255,255,.07);border:1px solid rgba(162,123,92,.25);
  border-radius:8px;color:var(--cr);font-size:.82rem;outline:none;cursor:pointer;font-family:inherit}
.sel option{background:#2c2018;color:var(--cr)}
.chk{display:flex;align-items:center;gap:.4rem;font-size:.82rem;cursor:pointer;
  color:rgba(245,239,230,.7)}
.chk input{accent-color:var(--br);cursor:pointer}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(230px,1fr));gap:1.25rem;
  padding:1.75rem 2rem;max-width:1400px;margin:0 auto}
.card{background:linear-gradient(160deg,#2a1f16,#231a12);border:1px solid var(--bd);
  border-radius:16px;overflow:hidden;cursor:pointer;transition:transform .25s,border-color .25s,box-shadow .25s;
  opacity:0;animation:fadeIn .4s forwards}
.card:hover{transform:translateY(-5px);border-color:var(--br);box-shadow:0 12px 32px rgba(162,123,92,.2)}
@keyframes fadeIn{to{opacity:1}}
.img-w{position:relative;width:100%;aspect-ratio:1/1;overflow:hidden;background:#1a1512}
.c-img{width:100%;height:100%;object-fit:cover;transition:transform .4s}
.card:hover .c-img{transform:scale(1.06)}
.img-err{width:100%;height:100%;display:flex;align-items:center;justify-content:center;
  font-size:3rem;opacity:.25}
.chip-cat{position:absolute;top:.6rem;left:.6rem;background:rgba(26,21,18,.75);
  color:rgba(245,239,230,.8);font-size:.6rem;font-weight:600;padding:.18rem .55rem;
  border-radius:99px;letter-spacing:.04em;backdrop-filter:blur(6px)}
.chip-sale{position:absolute;top:.6rem;right:.6rem;background:var(--sl);color:#fff;
  font-size:.6rem;font-weight:700;padding:.18rem .55rem;border-radius:99px}
.c-body{padding:1rem}
.c-name{font-family:'Playfair Display',serif;font-size:.95rem;font-weight:700;
  color:var(--cr);margin-bottom:.25rem;line-height:1.3}
.c-desc{font-size:.75rem;color:rgba(245,239,230,.5);line-height:1.55;margin-bottom:.5rem;
  display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}
.c-foot{display:flex;align-items:center;justify-content:space-between;margin-top:.625rem}
.p-now{font-family:'Playfair Display',serif;font-size:1rem;font-weight:700;font-style:italic;
  color:var(--br)}
.p-was{font-size:.75rem;color:rgba(245,239,230,.35);text-decoration:line-through;margin-left:.35rem}
.c-id{font-size:.65rem;color:rgba(162,123,92,.45)}
.empty{grid-column:1/-1;text-align:center;padding:5rem 0;color:rgba(245,239,230,.3);font-size:.9rem}
/* OVERLAY */
.ov{position:fixed;inset:0;background:rgba(10,8,6,.82);backdrop-filter:blur(8px);z-index:200;
  display:flex;align-items:flex-end;justify-content:center;opacity:0;pointer-events:none;
  transition:opacity .25s}
.ov.open{opacity:1;pointer-events:all}
.modal{background:linear-gradient(160deg,#2a1f16,#1e1610);border:1px solid var(--bd);
  border-radius:22px 22px 0 0;width:100%;max-width:560px;max-height:88vh;overflow-y:auto;
  transform:translateY(100%);transition:transform .35s cubic-bezier(.22,1,.36,1)}
.ov.open .modal{transform:translateY(0)}
.mclose{position:absolute;top:1rem;right:1rem;background:rgba(255,255,255,.1);border:none;
  color:var(--cr);font-size:1.25rem;width:32px;height:32px;border-radius:50%;cursor:pointer;
  display:flex;align-items:center;justify-content:center}
.mi-w{position:relative;width:100%;aspect-ratio:16/9;overflow:hidden;background:#1a1512}
.m-img{width:100%;height:100%;object-fit:cover}
.m-img-err{width:100%;height:100%;display:flex;align-items:center;justify-content:center;
  font-size:5rem;opacity:.2}
.mcat{font-size:.62rem;font-weight:700;letter-spacing:.18em;text-transform:uppercase;
  color:var(--br);margin-bottom:.5rem}
.mname{font-family:'Playfair Display',serif;font-size:1.5rem;font-weight:700;
  color:var(--cr);margin-bottom:.5rem;line-height:1.25}
.mprice{display:flex;align-items:baseline;gap:.5rem;margin-bottom:.875rem}
.m-price{font-family:'Playfair Display',serif;font-size:1.35rem;font-weight:700;
  font-style:italic;color:var(--br)}
.m-orig{font-size:.875rem;color:rgba(245,239,230,.35);text-decoration:line-through}
.m-spill{background:var(--sl);color:#fff;font-size:.68rem;font-weight:700;
  padding:.15rem .5rem;border-radius:99px}
.mdesc{font-size:.875rem;color:rgba(245,239,230,.7);line-height:1.75;
  padding:.875rem 0;border-top:1px solid rgba(162,123,92,.12);
  border-bottom:1px solid rgba(162,123,92,.12);margin-bottom:1rem}
.mmeta{display:grid;grid-template-columns:1fr 1fr;gap:.5rem;margin-bottom:1rem}
.m-mi{background:rgba(0,0,0,.2);border-radius:10px;padding:.625rem .875rem}
.m-ml{font-size:.62rem;font-weight:700;text-transform:uppercase;letter-spacing:.1em;
  color:rgba(162,123,92,.65);margin-bottom:3px}
.m-mv{font-size:.82rem;font-weight:600;color:var(--cr)}
.jsonbtn{background:rgba(162,123,92,.12);border:1px solid rgba(162,123,92,.25);
  color:rgba(245,239,230,.7);font-size:.75rem;padding:.4rem .875rem;border-radius:8px;
  cursor:pointer;font-family:inherit;transition:background .15s}
.jsonbtn:hover{background:rgba(162,123,92,.22)}
.jsonbox{display:none;margin-top:.625rem;background:#0d0b09;border:1px solid rgba(162,123,92,.2);
  border-radius:10px;padding:1rem;font-size:.72rem;color:rgba(245,239,230,.7);
  overflow-x:auto;white-space:pre;line-height:1.6;max-height:220px;overflow-y:auto}
.jsonbox.show{display:block}
@media(max-width:600px){.grid{grid-template-columns:repeat(2,1fr);gap:.875rem;padding:1rem}}";
        }

        private static string GetJs(string dataJson, int count)
        {
            // JS code - dung string concatenation, KHONG dung C# string interpolation
            // De tranh conflict voi regex JS /pattern/flags va single quotes
            var sb = new StringBuilder();

            // Data + count
            sb.Append("var D=").Append(dataJson).Append(";\n");
            sb.Append("var ALL=D.slice();\n");
            sb.Append("document.getElementById('cnt').textContent='").Append(count).Append(" san pham';\n");

            // Build category filter
            sb.AppendLine("(function(){");
            sb.AppendLine("  var cats={};");
            sb.AppendLine("  D.forEach(function(p){if(p.categoryName)cats[p.categoryId]=p.categoryName;});");
            sb.AppendLine("  var sel=document.getElementById('cat');");
            sb.AppendLine("  Object.keys(cats).forEach(function(k){");
            sb.AppendLine("    var o=document.createElement('option');");
            sb.AppendLine("    o.value=k; o.textContent=cats[k]; sel.appendChild(o);");
            sb.AppendLine("  });");
            sb.AppendLine("})();");

            // Helper: format price
            sb.AppendLine("function fmt(n){return Number(n).toLocaleString('vi-VN')+'\\u0111';}");

            // Helper: html escape
            sb.AppendLine("function esc(s){");
            sb.AppendLine("  return String(s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/\"/g,'&quot;');");
            sb.AppendLine("}");

            // render grid
            sb.AppendLine("function rg(ps){");
            sb.AppendLine("  var g=document.getElementById('grid');");
            sb.AppendLine("  document.getElementById('cnt').textContent=ps.length+' san pham';");
            sb.AppendLine("  if(!ps.length){g.innerHTML='<div class=\"empty\">Khong co san pham phu hop.</div>';return;}");
            sb.AppendLine("  g.innerHTML=ps.map(function(p,i){");
            sb.AppendLine("    var sale=p.isOnSale&&p.salePrice&&p.salePrice<p.price;");
            sb.AppendLine("    var dp=sale?p.salePrice:p.price;");
            sb.AppendLine("    var ih=p.imageUrl");
            sb.AppendLine("      ?'<img class=\"c-img\" src=\"'+esc(p.imageUrl)+'\" loading=\"lazy\" onerror=\"this.style.display=\\'none\\';this.nextElementSibling.style.display=\\'flex\\'\"/><div class=\"img-err\" style=\"display:none\">\\u2615</div>'");
            sb.AppendLine("      :'<div class=\"img-err\">\\u2615</div>';");
            sb.AppendLine("    var ph=sale");
            sb.AppendLine("      ?'<span class=\"p-now\">'+fmt(dp)+'</span><span class=\"p-was\">'+fmt(p.price)+'</span>'");
            sb.AppendLine("      :'<span class=\"p-now\">'+fmt(dp)+'</span>';");
            sb.AppendLine("    return '<div class=\"card\" style=\"animation-delay:'+(i*30)+'ms\" onclick=\"om('+p.id+')\">'");
            sb.AppendLine("      +'<div class=\"img-w\">'+ih");
            sb.AppendLine("      +(p.categoryName?'<span class=\"chip-cat\">'+esc(p.categoryName)+'</span>':'')");
            sb.AppendLine("      +(sale?'<span class=\"chip-sale\">\\u2212'+p.discountPercent+'%</span>':'')");
            sb.AppendLine("      +'</div><div class=\"c-body\">'");
            sb.AppendLine("      +'<div class=\"c-name\">'+esc(p.name)+'</div>'");
            sb.AppendLine("      +(p.description?'<div class=\"c-desc\">'+esc(p.description)+'</div>':'')");
            sb.AppendLine("      +'<div class=\"c-foot\"><div>'+ph+'</div><span class=\"c-id\">#'+p.id+'</span></div>'");
            sb.AppendLine("      +'</div></div>';");
            sb.AppendLine("  }).join('');");
            sb.AppendLine("}");

            // Filter + sort
            sb.AppendLine("function fil(){");
            sb.AppendLine("  var q=document.getElementById('q').value.toLowerCase();");
            sb.AppendLine("  var cat=document.getElementById('cat').value;");
            sb.AppendLine("  var srt=document.getElementById('srt').value;");
            sb.AppendLine("  var saleOnly=document.getElementById('sale').checked;");
            sb.AppendLine("  var ps=ALL.filter(function(p){");
            sb.AppendLine("    if(q&&p.name.toLowerCase().indexOf(q)<0)return false;");
            sb.AppendLine("    if(cat&&String(p.categoryId)!==cat)return false;");
            sb.AppendLine("    if(saleOnly&&!p.isOnSale)return false;");
            sb.AppendLine("    return true;");
            sb.AppendLine("  });");
            sb.AppendLine("  if(srt==='pa')ps.sort(function(a,b){return (a.isOnSale?a.salePrice:a.price)-(b.isOnSale?b.salePrice:b.price);});");
            sb.AppendLine("  else if(srt==='pd')ps.sort(function(a,b){return (b.isOnSale?b.salePrice:b.price)-(a.isOnSale?a.salePrice:a.price);});");
            sb.AppendLine("  else if(srt==='na')ps.sort(function(a,b){return a.name.localeCompare(b.name);});");
            sb.AppendLine("  rg(ps);");
            sb.AppendLine("}");

            // Open modal
            sb.AppendLine("function om(id){");
            sb.AppendLine("  var p=D.find(function(x){return x.id===id;});if(!p)return;");
            sb.AppendLine("  var sale=p.isOnSale&&p.salePrice&&p.salePrice<p.price;");
            sb.AppendLine("  document.getElementById('mi').innerHTML=p.imageUrl");
            sb.AppendLine("    ?'<img class=\"m-img\" src=\"'+esc(p.imageUrl)+'\" onerror=\"this.style.display=\\'none\\';this.nextElementSibling.style.display=\\'flex\\'\"/><div class=\"m-img-err\" style=\"display:none\">\\u2615</div>'");
            sb.AppendLine("    :'<div class=\"m-img-err\">\\u2615</div>';");
            sb.AppendLine("  document.getElementById('mc').textContent=p.categoryName||'';");
            sb.AppendLine("  document.getElementById('mn').textContent=p.name;");
            sb.AppendLine("  document.getElementById('mp').innerHTML=sale");
            sb.AppendLine("    ?'<span class=\"m-price\">'+fmt(p.salePrice)+'</span><span class=\"m-orig\">'+fmt(p.price)+'</span><span class=\"m-spill\">\\u2212'+p.discountPercent+'%</span>'");
            sb.AppendLine("    :'<span class=\"m-price\">'+fmt(p.price)+'</span>';");
            sb.AppendLine("  document.getElementById('md').textContent=p.description||'Chua co mo ta.';");
            sb.AppendLine("  document.getElementById('mm').innerHTML=");
            sb.AppendLine("    '<div class=\"m-mi\"><div class=\"m-ml\">ID</div><div class=\"m-mv\">#'+p.id+'</div></div>'");
            sb.AppendLine("    +'<div class=\"m-mi\"><div class=\"m-ml\">Danh muc</div><div class=\"m-mv\">'+(p.categoryName||'\\u2014')+'</div></div>'");
            sb.AppendLine("    +'<div class=\"m-mi\"><div class=\"m-ml\">Trang thai</div><div class=\"m-mv\" style=\"color:'+(sale?'#e05c4b':'#4caf50')+'\">'+(sale?'\\ud83d\\udd25 Dang sale':'\\u2705 Binh thuong')+'</div></div>'");
            sb.AppendLine("    +'<div class=\"m-mi\"><div class=\"m-ml\">Anh phu</div><div class=\"m-mv\">'+(p.imageCount||0)+'</div></div>';");
            sb.AppendLine("  document.getElementById('jb').textContent=JSON.stringify(p,null,2);");
            sb.AppendLine("  document.getElementById('jb').classList.remove('show');");
            sb.AppendLine("  document.getElementById('ov').classList.add('open');");
            sb.AppendLine("  document.body.style.overflow='hidden';");
            sb.AppendLine("}");

            // Close modal
            sb.AppendLine("function cm(){document.getElementById('ov').classList.remove('open');document.body.style.overflow='';}");

            // Toggle JSON
            sb.AppendLine("function tj(){document.getElementById('jb').classList.toggle('show');}");

            // ESC key
            sb.AppendLine("document.addEventListener('keydown',function(e){if(e.key==='Escape')cm();});");

            // Init — reset filter inputs to prevent browser autofill causing empty results
            sb.AppendLine("document.getElementById('q').value='';");
            sb.AppendLine("document.getElementById('cat').value='';");
            sb.AppendLine("document.getElementById('srt').value='';");
            sb.AppendLine("document.getElementById('sale').checked=false;");
            sb.AppendLine("rg(D);");

            return sb.ToString();
        }
    }
}