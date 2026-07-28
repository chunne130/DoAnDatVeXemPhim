using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DoAnDatVeXemPhim.Models;
using System.Linq;

namespace DoAnDatVeXemPhim.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Genre> Genres { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<CinemaHall> CinemaHalls { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Showtime> Showtimes { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OrderCombo> OrderCombos { get; set; }
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }

        // mới bổ sung 
        public DbSet<MembershipLevel> MembershipLevels { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<MovieFavorite> MovieFavorites { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<AssociationRule> AssociationRules { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<MovieReview> MovieReviews { get; set; }

        // --- MARKETING & TƯƠNG TÁC KHÁCH HÀNG ---
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserPromotion> UserPromotions { get; set; }
        public DbSet<EmailCampaign> EmailCampaigns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Giữ lại cái này để không bị lỗi Multiple Cascade Paths trong SQL
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}