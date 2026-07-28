using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Data
{
    public static class SeedData
    {
        // Hàm 1: Seed dữ liệu Phim, Rạp, Ghế, Combo 
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                SeedNewCombosAndTransactions(context);

                if (context.Movies.Any()) return;

                // 1. Seed Thể loại
                var hanhDong = new Genre { Name = "Hành động" };
                var hoatHinh = new Genre { Name = "Hoạt hình" };
                var kinhDi = new Genre { Name = "Kinh dị" };
                context.Genres.AddRange(hanhDong, hoatHinh, kinhDi);
                context.SaveChanges();

                // 1.5 Seed Đạo diễn & Diễn viên
                var dirDenis = new Director { Name = "Denis Villeneuve", ProfilePictureUrl = "https://image.tmdb.org/t/p/w200/zdqOaaJEnE8o26l21QoWp5l0u7U.jpg" };
                var dirMike = new Director { Name = "Mike Mitchell", ProfilePictureUrl = "https://image.tmdb.org/t/p/w200/A0H3Kk4r70d5Rk7O581Zq5JbMhN.jpg" };
                context.Directors.AddRange(dirDenis, dirMike);

                var actorTim = new Actor { Name = "Timothée Chalamet", ProfilePictureUrl = "https://image.tmdb.org/t/p/w200/8tJVcs4zX5v20Rwe4Z8c5e6r4OQ.jpg" };
                var actorZen = new Actor { Name = "Zendaya", ProfilePictureUrl = "https://image.tmdb.org/t/p/w200/jE3LgE8hLqLktX5b1oX1jOq1dY3.jpg" };
                var actorJack = new Actor { Name = "Jack Black", ProfilePictureUrl = "https://image.tmdb.org/t/p/w200/rtCx0fiYxJVhzXXdwZE2XRTfIKE.jpg" };
                context.Actors.AddRange(actorTim, actorZen, actorJack);
                context.SaveChanges();

                // 2. Seed Phim
                context.Movies.AddRange(
                    new Movie
                    {
                        Title = "Dune: Hành Tinh Cát",
                        Genres = new List<Genre> { hanhDong },
                        Directors = new List<Director> { dirDenis },
                        Actors = new List<Actor> { actorTim, actorZen },
                        Duration = 155,
                        ReleaseDate = DateTime.Now,
                        PosterUrl = "https://image.tmdb.org/t/p/w500/d5NXSklZfs7Z1oAWa7OqyHNCpUe.jpg",
                        TrailerUrl = "https://www.youtube.com/embed/n9xhJrPXop4",
                        AgeRestriction = "13+"
                    },
                    new Movie
                    {
                        Title = "Kung Fu Panda 4",
                        Genres = new List<Genre> { hoatHinh },
                        Directors = new List<Director> { dirMike },
                        Actors = new List<Actor> { actorJack },
                        Duration = 94,
                        ReleaseDate = DateTime.Now,
                        PosterUrl = "https://image.tmdb.org/t/p/w500/kDp1vUBiRSToMvsnqbebyqDbn8U.jpg",
                        TrailerUrl = "https://www.youtube.com/embed/RfH6N_H5G4U",
                        AgeRestriction = "P"
                    }
                );
                context.SaveChanges();

                // 3. Seed Phòng chiếu & 100 Ghế
                var phong1 = new CinemaHall { Name = "Phòng chiếu 01 (IMAX)", TotalSeats = 100 };
                context.CinemaHalls.Add(phong1);
                context.SaveChanges();

                char[] rows = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J' };
                foreach (var r in rows)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        context.Seats.Add(new Seat
                        {
                            SeatNumber = $"{r}{i}",
                            CinemaHallId = phong1.Id,
                            SeatType = (r == 'E' || r == 'F') ? "VIP" : "Normal"
                        });
                    }
                }

                // 4. Seed Combo
                context.Combos.AddRange(
                    new Combo { Name = "Combo Solo", Description = "1 Bắp ngọt lớn + 1 Nước siêu lớn", Price = 75000, ImageUrl = "https://www.cgv.vn/media/concession/2023/04/18/my_combo.png" },
                    new Combo { Name = "Combo Couple", Description = "1 Bắp lớn + 2 Nước lớn", Price = 95000, ImageUrl = "https://www.cgv.vn/media/concession/2023/04/18/couple_combo_1.png" }
                );

                context.SaveChanges();
            }
        }

        // Hàm 2: Seed Quyền và Tài khoản Admin (QUAY VỀ IdentityUser)
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            // TRẢ VỀ: Sử dụng IdentityUser mặc định
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames = { "Admin", "Customer", "Staff" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminEmail = "admin@cinema.com";
            var user = await userManager.FindByEmailAsync(adminEmail);

            if (user == null)
            {
                // TRẢ VỀ: Chỉ dùng IdentityUser với các trường cơ bản
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createAdmin = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Tạo tài khoản Nhân viên (Staff) mẫu
            var staffEmail = "staff@cinema.com";
            var staffUser = await userManager.FindByEmailAsync(staffEmail);
            if (staffUser == null)
            {
                var newStaff = new IdentityUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    EmailConfirmed = true
                };

                var createStaff = await userManager.CreateAsync(newStaff, "Staff@123");
                if (createStaff.Succeeded)
                {
                    await userManager.AddToRoleAsync(newStaff, "Staff");
                }
            }
        }

        public static void CleanDuplicateCombos(ApplicationDbContext context)
        {
            // 1. Đồng bộ "Bắp rang ngọt (Rìu)" sang "Bắp rang ngọt"
            var bapNgotRiu = context.Combos.FirstOrDefault(c => c.Name == "Bắp rang ngọt (Rìu)");
            var bapNgotThuong = context.Combos.FirstOrDefault(c => c.Name == "Bắp rang ngọt");
            if (bapNgotRiu != null && bapNgotThuong != null)
            {
                var orderCombos = context.OrderCombos.Where(oc => oc.ComboId == bapNgotRiu.Id).ToList();
                foreach (var oc in orderCombos)
                {
                    oc.ComboId = bapNgotThuong.Id;
                }
                context.Combos.Remove(bapNgotRiu);
                context.SaveChanges();
            }

            // 2. Tìm các combo bị lặp tên (không phân biệt chữ hoa thường)
            var allCombos = context.Combos.ToList();
            var grouped = allCombos.GroupBy(c => c.Name.Trim().ToLower()).Where(g => g.Count() > 1).ToList();
            foreach (var group in grouped)
            {
                var list = group.OrderBy(c => c.Id).ToList();
                var original = list[0]; // Giữ lại cái đầu tiên
                for (int i = 1; i < list.Count; i++)
                {
                    var duplicate = list[i];
                    var orderCombos = context.OrderCombos.Where(oc => oc.ComboId == duplicate.Id).ToList();
                    foreach (var oc in orderCombos)
                    {
                        oc.ComboId = original.Id;
                    }
                    context.Combos.Remove(duplicate);
                }
                context.SaveChanges();
            }
        }

        public static void SeedNewCombosAndTransactions(ApplicationDbContext context)
        {
            // 1. Dọn dẹp các combo trùng lặp trước
            CleanDuplicateCombos(context);

            // 2. Seed từng combo riêng lẻ nếu chưa có (so khớp tên không phân biệt hoa thường)
            var existingNames = context.Combos.Select(c => c.Name.ToLower().Trim()).ToHashSet();

            void AddComboIfMissing(string name, string desc, decimal price, string img)
            {
                if (!existingNames.Contains(name.ToLower().Trim()))
                {
                    context.Combos.Add(new Combo { Name = name, Description = desc, Price = price, ImageUrl = img });
                }
            }

            AddComboIfMissing("Bắp rang ngọt", "1 Hộp bắp rang ngọt truyền thống", 50000, "https://images.unsplash.com/photo-1518047601542-79f18c655718?w=300");
            AddComboIfMissing("Bắp rang phô mai", "1 Hộp bắp rang vị phô mai đậm đà", 60000, "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=300");
            AddComboIfMissing("Nước ngọt Pepsi lon", "Pepsi lon lạnh sảng khoái", 25000, "https://images.unsplash.com/photo-1531384441138-2736e62e0919?w=300");
            AddComboIfMissing("Nước ngọt Sprite lon", "Sprite lon lạnh sảng khoái", 25000, "https://images.unsplash.com/photo-1625772290748-160b2a688b52?w=300");
            AddComboIfMissing("Khăn ướt Cinema Hub", "Khăn ướt lau tay cao cấp", 5000, "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=300");

            context.SaveChanges();

            // 3. Sửa lại ảnh bị hỏng của các combo cũ trong DB
            var combos = context.Combos.ToList();
            foreach (var c in combos)
            {
                if (string.IsNullOrEmpty(c.ImageUrl) || c.ImageUrl.Contains("cgv.vn") || c.ImageUrl.Contains("via.placeholder"))
                {
                    if (c.Name.Contains("Solo")) c.ImageUrl = "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=300";
                    else if (c.Name.Contains("Couple")) c.ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=300";
                    else if (c.Name.Contains("ngọt")) c.ImageUrl = "https://images.unsplash.com/photo-1518047601542-79f18c655718?w=300";
                    else if (c.Name.Contains("phô mai")) c.ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=300";
                    else if (c.Name.Contains("Pepsi")) c.ImageUrl = "https://images.unsplash.com/photo-1531384441138-2736e62e0919?w=300";
                    else if (c.Name.Contains("Sprite")) c.ImageUrl = "https://images.unsplash.com/photo-1625772290748-160b2a688b52?w=300";
                    else if (c.Name.Contains("Khăn")) c.ImageUrl = "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?w=300";
                }
            }
            context.SaveChanges();

            // 4. Chỉ sinh giao dịch giả lập nếu đã có Phim, Phòng chiếu, và User
            if (context.Movies.Any() && context.CinemaHalls.Any() && context.Users.Any())
            {
                if (context.Orders.Count(o => o.IsPaid) < 20)
                {
                    GenerateMockTransactions(context);
                }
            }
        }

        public static void GenerateMockTransactions(ApplicationDbContext context)
        {
            var user = context.Users.FirstOrDefault();
            if (user == null) return;

            var showtime = context.Showtimes.FirstOrDefault();
            if (showtime == null)
            {
                var movie = context.Movies.FirstOrDefault();
                var hall = context.CinemaHalls.FirstOrDefault();
                if (movie == null || hall == null) return;
                showtime = new Showtime
                {
                    MovieId = movie.Id,
                    CinemaHallId = hall.Id,
                    StartTime = DateTime.Now.AddDays(1),
                    EndTime = DateTime.Now.AddDays(1).AddHours(2),
                    BasePrice = 80000,
                    Format = "2D",
                    IsActive = true
                };
                context.Showtimes.Add(showtime);
                context.SaveChanges();
            }

            var seats = context.Seats.Where(s => s.CinemaHallId == showtime.CinemaHallId).Take(5).ToList();
            if (seats.Count == 0) return;

            var combos = context.Combos.ToList();
            var comboSolo = combos.FirstOrDefault(c => c.Name == "Combo Solo");
            var comboCouple = combos.FirstOrDefault(c => c.Name == "Combo Couple");
            var popcornSweet = combos.FirstOrDefault(c => c.Name == "Bắp rang ngọt (Rìu)");
            var popcornCheese = combos.FirstOrDefault(c => c.Name == "Bắp rang phô mai");
            var pepsi = combos.FirstOrDefault(c => c.Name == "Nước ngọt Pepsi lon");
            var sprite = combos.FirstOrDefault(c => c.Name == "Nước ngọt Sprite lon");
            var tissue = combos.FirstOrDefault(c => c.Name == "Khăn ướt Cinema Hub");

            var random = new Random();

            for (int i = 0; i < 60; i++)
            {
                var order = new Order
                {
                    OrderDate = DateTime.Now.AddDays(-random.Next(1, 30)),
                    UserId = user.Id,
                    IsPaid = true,
                    Status = "PAID",
                    PaymentMethod = "Momo",
                    TotalAmount = 0
                };
                context.Orders.Add(order);
                context.SaveChanges();

                var seat = seats[random.Next(seats.Count)];
                context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    ShowtimeId = showtime.Id,
                    SeatId = seat.Id,
                    PriceAtBooking = showtime.BasePrice
                });

                decimal orderTotal = showtime.BasePrice;
                int pattern = random.Next(1, 5);

                if (pattern == 1 && popcornSweet != null && pepsi != null)
                {
                    context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = popcornSweet.Id, Quantity = 1, Price = popcornSweet.Price });
                    orderTotal += popcornSweet.Price;

                    if (random.NextDouble() < 0.8)
                    {
                        context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = pepsi.Id, Quantity = 1, Price = pepsi.Price });
                        orderTotal += pepsi.Price;
                    }
                    if (random.NextDouble() < 0.7 && tissue != null)
                    {
                        context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = tissue.Id, Quantity = 1, Price = tissue.Price });
                        orderTotal += tissue.Price;
                    }
                }
                else if (pattern == 2 && popcornCheese != null && sprite != null)
                {
                    context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = popcornCheese.Id, Quantity = 1, Price = popcornCheese.Price });
                    orderTotal += popcornCheese.Price;

                    if (random.NextDouble() < 0.75)
                    {
                        context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = sprite.Id, Quantity = 1, Price = sprite.Price });
                        orderTotal += sprite.Price;
                    }
                }
                else if (pattern == 3 && comboCouple != null && tissue != null)
                {
                    context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = comboCouple.Id, Quantity = 1, Price = comboCouple.Price });
                    orderTotal += comboCouple.Price;

                    if (random.NextDouble() < 0.6)
                    {
                        context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = tissue.Id, Quantity = 2, Price = tissue.Price });
                        orderTotal += tissue.Price * 2;
                    }
                }
                else if (comboSolo != null)
                {
                    context.OrderCombos.Add(new OrderCombo { OrderId = order.Id, ComboId = comboSolo.Id, Quantity = 1, Price = comboSolo.Price });
                    orderTotal += comboSolo.Price;
                }

                order.TotalAmount = orderTotal;
                context.Orders.Update(order);
            }
            context.SaveChanges();
        }
    }
}