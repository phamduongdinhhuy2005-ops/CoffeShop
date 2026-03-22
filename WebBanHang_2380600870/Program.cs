// Program.cs
// FIXED:
// Bug 11 — OAuth registration: identityBuilder.Services.AddAuthentication() là sai API
//           IdentityBuilder không có property .Services để chain thêm provider
//           Cần dùng builder.Services.AddAuthentication() sau khi AddIdentity đã chạy
// Bug 12 — Admin seed FullName: "Quản Trị Viên" bị lưu dưới dạng chuỗi mojibake
//           Sửa thành string literal UTF-8 đúng chuẩn
// (Giữ nguyên: OAuth optional check, Session trước Routing, cookie config)

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang_2380600870.Models;
using WebBanHang_2380600870.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ===== DATABASE =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== IDENTITY =====
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ===== GOOGLE + FACEBOOK OAUTH =====
// Chỉ đăng ký nếu key tồn tại — người clone về chưa có key vẫn chạy được
// Để bật: thêm vào appsettings.json hoặc User Secrets:
//   "Authentication": {
//     "Google":   { "ClientId": "...", "ClientSecret": "..." },
//     "Facebook": { "AppId":    "...", "AppSecret":    "..." }
//   }
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
var fbAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

// FIX Bug 11: Dùng builder.Services.AddAuthentication() — KHÔNG phải identityBuilder.Services
// AddIdentity() đã tự gọi AddAuthentication() bên trong và set DefaultScheme.
// Ta chỉ cần chain thêm provider qua builder.Services, không tạo builder mới.
var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

if (!string.IsNullOrEmpty(fbAppId) && !string.IsNullOrEmpty(fbAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = fbAppId;
        options.AppSecret = fbAppSecret;
    });
}

// ===== COOKIE =====
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ===== SESSION =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// ===== REPOSITORIES =====
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ===== SEED ROLES + ADMIN =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@goclang.vn";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Quản Trị Viên",   // FIX Bug 12: chuỗi UTF-8 đúng chuẩn
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
// Session phải đứng trước Routing để middleware pipeline xử lý đúng
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();