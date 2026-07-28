using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH DATABASE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. ĐĂNG KÝ CÁC SERVICE 
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Đăng ký dịch vụ gọi API bên ngoài
builder.Services.AddHttpClient();

// ĐĂNG KÝ THANH TOÁN PAYOS VÀ NOTIFICATION
builder.Services.AddScoped<DoAnDatVeXemPhim.Services.ThanhToanService>();
builder.Services.AddScoped<DoAnDatVeXemPhim.Services.AprioriService>();
builder.Services.AddScoped<DoAnDatVeXemPhim.Services.NotificationService>();

// Background Job - Email Marketing
builder.Services.AddHostedService<DoAnDatVeXemPhim.Services.EmailMarketingJob>();

// 3. CẤU HÌNH IDENTITY
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// 🔑 CẤU HÌNH GOOGLE OAUTH
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });

// 4. CẤU HÌNH SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 5. CẤU HÌNH BLAZOR & MVC
builder.Services.AddServerSideBlazor();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// 6. CẤU HÌNH COOKIE ĐĂNG NHẬP
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

// --- KẾT THÚC ĐĂNG KÝ DỊCH VỤ ---
var app = builder.Build();
// --------------------------------

// 7. CẤU HÌNH HTTP REQUEST PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.MapBlazorHub();

// --- THỨ TỰ MIDDLEWARE: Authentication -> Session -> Authorization ---
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<DoAnDatVeXemPhim.Hubs.SeatHub>("/seatHub");
app.MapHub<DoAnDatVeXemPhim.Hubs.NotificationHub>("/notificationHub");

// 8. TỰ ĐỘNG NẠP DỮ LIỆU (SEED DATA)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        SeedData.Initialize(services);
        SeedData.SeedRolesAndAdminAsync(services).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi nạp dữ liệu mẫu.");
    }
}

app.Run();