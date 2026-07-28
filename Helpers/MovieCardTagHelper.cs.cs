using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DoAnDatVeXemPhim.Helpers
{
    [HtmlTargetElement("movie-card")]
    public class MovieCardTagHelper : TagHelper
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public int Duration { get; set; }
        public string GenreName { get; set; }
        public bool IsHot { get; set; } = true;
        
        // --- ĐÃ THÊM: Tính năng Behavior Tracking ---
        public int ViewCount { get; set; }
        
        // --- ĐÃ THÊM: Tính năng Đánh giá phim ---
        public double AverageRating { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "col-6 col-md-4 col-lg-3 mb-4");

            string hotBadge = IsHot ? "<span class='badge bg-danger mb-1' style='font-size:0.6rem;letter-spacing:0.05em;'>HOT</span>" : "";
            
            // Xử lý hiển thị sao (chỉ hiện nếu có đánh giá) đặt trên góc poster
            string ratingBadge = "";
            if (AverageRating > 0)
            {
                ratingBadge = $"<div style='position: absolute; bottom: 8px; right: 8px; background-color: rgba(0, 0, 0, 0.75); color: #fbbf24; padding: 3px 8px; border-radius: 6px; font-weight: bold; font-size: 0.85rem; border: 1px solid rgba(255,255,255,0.2); z-index: 2; display: flex; align-items: center;'><i class='bi bi-star-fill me-1'></i>{AverageRating:F1}</div>";
            }

            string content = $@"
                <div class='movie-card'>
                    <div class='poster-container shadow' style='position: relative;'>
                        <img src='{PosterUrl}' class='img-fluid w-100' style='aspect-ratio:2/3;object-fit:cover;display:block;' alt='{Title}' onload=""this.classList.add('loaded')"">
                        {ratingBadge}
                        <div class='movie-overlay'>
                            {hotBadge}
                            <a href='/Booking/SelectShowtime?movieId={Id}' class='btn-overlay-action btn-overlay-book'>Đặt vé</a>
                            <a href='/Home/MovieDetails/{Id}' class='btn-overlay-action btn-overlay-detail'>Chi tiết</a>
                        </div>
                    </div>
                    <div class='movie-title-text'>{Title}</div>
                    
                    <!-- Tính năng Behavior Tracking ViewCount -->
                    <div class='d-flex justify-content-between align-items-center mt-1' style='padding: 0 10px 10px 10px;'>
                        <small class='text-muted'><i class='bi bi-clock me-1'></i> {Duration} phút</small>
                        <span class='fw-bold' style='color:#fbbf24; font-size:0.75rem;'><i class='bi bi-eye-fill me-1'></i>{ViewCount} lượt xem</span>
                    </div>

                    <!-- Nút thao tác riêng cho Mobile -->
                    <div class='movie-mobile-actions'>
                        <a href='/Booking/SelectShowtime?movieId={Id}' class='btn btn-success text-dark w-100 mb-1'>Đặt vé</a>
                        <a href='/Home/MovieDetails/{Id}' class='btn btn-outline-light w-100'>Chi tiết</a>
                    </div>
                </div>";

            output.Content.SetHtmlContent(content);
        }
    }
}