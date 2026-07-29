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
        public int ViewCount { get; set; }
        public double AverageRating { get; set; }

        // Inject CSS only once per page using a static flag per request
        private static bool _cssInjected = false;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "col-6 col-md-4 col-lg-3");

            // CSS block — chỉ cần inject 1 lần nhưng để idempotent, browser bỏ qua duplicate
            string css = @"<style>
.mc-wrap{position:relative;border-radius:18px;overflow:hidden;background:rgba(255,255,255,0.02);border:1px solid rgba(255,255,255,0.06);transition:all 0.4s cubic-bezier(0.175,0.885,0.32,1.275);cursor:pointer;display:flex;flex-direction:column;height:100%;}
.mc-wrap:hover{transform:translateY(-10px) scale(1.01);border-color:rgba(0,255,135,0.35);box-shadow:0 25px 50px rgba(0,0,0,0.5),0 0 0 1px rgba(0,255,135,0.1),0 0 30px rgba(0,255,135,0.08);}
.mc-poster-wrap{position:relative;padding-top:150%;overflow:hidden;background:linear-gradient(135deg,#0d1f10,#1a3320);}
.mc-poster-wrap img{position:absolute;top:0;left:0;width:100%;height:100%;object-fit:cover;opacity:0;transition:transform 0.5s ease,opacity 0.3s ease;}
.mc-poster-wrap img.mc-loaded{opacity:1;}
.mc-wrap:hover .mc-poster-wrap img{transform:scale(1.08);}
.mc-hot-badge{position:absolute;top:12px;right:12px;background:linear-gradient(135deg,#ff416c,#ff4b2b);color:#fff;padding:3px 10px;border-radius:6px;font-weight:700;font-size:0.7rem;z-index:2;}
.mc-overlay{position:absolute;inset:0;background:linear-gradient(to top,rgba(0,0,0,0.95) 0%,rgba(0,0,0,0.4) 55%,transparent 100%);opacity:0;transition:opacity 0.35s ease;display:flex;flex-direction:column;align-items:center;justify-content:flex-end;padding:16px;gap:8px;}
.mc-wrap:hover .mc-overlay{opacity:1;}
.mc-btn-p,.mc-btn-o{width:100%;padding:9px 0;font-size:0.82rem;font-weight:700;border-radius:50px;text-align:center;text-decoration:none;display:block;transform:translateY(12px);opacity:0;transition:all 0.3s ease;}
.mc-wrap:hover .mc-btn-p{opacity:1;transform:translateY(0);transition-delay:0.06s;}
.mc-wrap:hover .mc-btn-o{opacity:1;transform:translateY(0);transition-delay:0s;}
.mc-btn-p{background:#00ff87;color:#022c22!important;border:none;}
.mc-btn-p:hover{background:#00e676;color:#022c22!important;}
.mc-btn-o{background:rgba(255,255,255,0.1);color:#fff!important;border:1px solid rgba(255,255,255,0.3);}
.mc-btn-o:hover{background:rgba(255,255,255,0.2);}
.mc-body{padding:14px 14px 16px;display:flex;flex-direction:column;gap:8px;flex:1;}
.mc-title{font-size:0.95rem;font-weight:700;color:#fff;margin:0;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;transition:color 0.2s;line-height:1.4;}
.mc-wrap:hover .mc-title{color:#00ff87;}
.mc-genres{display:flex;flex-wrap:wrap;gap:4px;min-height:22px;}
.mc-genre-tag{font-size:0.7rem;padding:2px 8px;border-radius:50px;background:rgba(0,255,135,0.07);color:#4ade80;border:1px solid rgba(0,255,135,0.15);}
.mc-meta{display:flex;align-items:center;justify-content:space-between;font-size:0.78rem;color:#6b7280;margin-top:auto;}
.mc-star{color:#f59e0b;font-weight:700;}
.mc-ticket-wrap{margin-top:4px;}
.mc-ticket-lbl{font-size:0.7rem;color:#6b7280;margin-bottom:4px;}
.mc-ticket-bar{height:3px;background:rgba(255,255,255,0.08);border-radius:3px;overflow:hidden;}
.mc-ticket-fill{height:100%;background:linear-gradient(90deg,#00ff87,#00e676);border-radius:3px;}
</style>";

            // HOT badge
            string hotBadge = ViewCount > 20
                ? "<span class='mc-hot-badge'>🔥 HOT</span>"
                : "";

            // Rating
            string ratingHtml = AverageRating > 0
                ? $"<span class='mc-star'><i class='bi bi-star-fill me-1'></i>{AverageRating:F1}</span>"
                : "<span style='color:#6b7280'>—</span>";

            // Genre pills (max 2)
            string genrePills = "";
            if (!string.IsNullOrEmpty(GenreName))
            {
                foreach (var g in GenreName.Split(',').Take(2))
                    genrePills += $"<span class='mc-genre-tag'>{g.Trim()}</span>";
            }

            // Ticket bar
            string ticketBar = "";
            if (ViewCount > 0)
            {
                int pct = Math.Min(ViewCount, 100);
                ticketBar = $@"<div class='mc-ticket-wrap'>
                    <div class='mc-ticket-lbl'><i class='bi bi-ticket-perforated me-1'></i>{ViewCount} vé đã bán</div>
                    <div class='mc-ticket-bar'><div class='mc-ticket-fill' style='width:{pct}%'></div></div>
                </div>";
            }

            string content = $@"{css}
<div class='mc-wrap'>
    <div class='mc-poster-wrap' id='mcpw-{Id}'>
        <img src='{PosterUrl}' alt='{System.Web.HttpUtility.HtmlEncode(Title)}'
             loading='lazy'
             onload=""this.classList.add('mc-loaded'); document.getElementById('mcpw-{Id}').classList.add('img-loaded')""
             onerror=""this.src='/img/placeholder.jpg'; this.classList.add('mc-loaded')"" />
        {hotBadge}
        <div class='mc-overlay'>
            <a href='/Booking/SelectShowtime?movieId={Id}' class='mc-btn-p'>
                <i class='bi bi-ticket-perforated me-2'></i>Đặt vé ngay
            </a>
            <a href='/Home/MovieDetails/{Id}' class='mc-btn-o'>
                <i class='bi bi-info-circle me-2'></i>Xem chi tiết
            </a>
        </div>
    </div>
    <div class='mc-body'>
        <a href='/Home/MovieDetails/{Id}' class='text-decoration-none'>
            <h6 class='mc-title'>{System.Web.HttpUtility.HtmlEncode(Title)}</h6>
        </a>
        <div class='mc-genres'>{genrePills}</div>
        <div class='mc-meta'>
            <div>{ratingHtml}</div>
            <span><i class='bi bi-clock me-1'></i>{Duration} phút</span>
        </div>
        {ticketBar}
    </div>
</div>";

            output.Content.SetHtmlContent(content);
        }
    }
}