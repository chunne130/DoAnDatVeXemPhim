namespace DoAnDatVeXemPhim.Models
{
    // Bảng quản lý Mã giảm giá(Voucher) áp dụng khi thanh toán Orders
    public class Promotion
    {
        public int Id { get; set; }
        public string VoucherCode { get; set; } // Ví dụ: MOVIE2026
        public string Description { get; set; }
        public decimal DiscountValue { get; set; } // Số tiền giảm hoặc phần trăm giảm
        public bool IsPercentage { get; set; } // True nếu là giảm %, False nếu trừ thẳng tiền
        public decimal MinOrderValue { get; set; } // Giá trị đơn hàng tối thiểu để áp dụng mã
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UsageLimit { get; set; } // Giới hạn tổng số lượt xài
        public int UsedCount { get; set; } = 0; // Số lượt đã xài

        // ─── ĐÃ THÊM: LIÊN KẾT ĐỐI TƯỢNG HẠNG THÀNH VIÊN ĐƯỢC ÁP DỤNG ───
        public int? MembershipLevelId { get; set; } // Khóa ngoại nullable (để trống = tất cả mọi người xài được)
        public virtual MembershipLevel? MembershipLevel { get; set; } // Navigation property liên kết ngầm

        // ─── ĐIỂM THƯỞNG YÊU CẦU ĐỂ ĐỔI VOUCHER NÀY ───
        public int PointsRequired { get; set; } = 0; // Số điểm cần thiết để đổi, nếu = 0 thì không cho đổi bằng điểm
    }
}
