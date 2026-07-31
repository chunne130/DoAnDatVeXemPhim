using DoAnDatVeXemPhim.Data;
using DoAnDatVeXemPhim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ChatbotController(ApplicationDbContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _apiKey = configuration["GeminiSetting:ApiKey"];
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message, int? sessionId)
        {
            if (string.IsNullOrEmpty(message))
                return Json(new { success = false, error = "Tin nhắn trống" });

            string botReply = "Cinema Hub xin chào! Bạn muốn xem phim gì nè? 💕";
            int returnedSessionId = sessionId ?? 0;

            try
            {
                //  TRUY VẤN DỮ LIỆU PHIM 
                var dbMovies = await _context.Movies
                                             .Include(m => m.Genres)
                                             .Include(m => m.Showtimes)
                                             .AsNoTracking()
                                             .ToListAsync();

                DateTime realtimeNow = DateTime.Now;

                //  XÂY DỰNG NGỮ CẢNH
                StringBuilder systemContext = new StringBuilder();
                systemContext.AppendLine("QUY ĐỊNH RẠP:");
                systemContext.AppendLine("- Giá vé gốc áp dụng cho Ghế Thường. Phụ thu Ghế VIP: +20.000đ. Phụ thu Ghế Cặp đôi (Sweetbox): +50.000đ.");
                systemContext.AppendLine("- Giảm 10% tổng tiền cho đối tượng Học Sinh - Sinh Viên.");
                systemContext.AppendLine($"THỜI GIAN HIỆN TẠI (Thực tế): {realtimeNow.ToString("HH:mm dd/MM/yyyy")}");
                systemContext.AppendLine("LỊCH CHIẾU & THÔNG TIN PHIM:");

                int countValidMovies = 0;

                if (dbMovies != null && dbMovies.Any())
                {
                    foreach (var m in dbMovies)
                    {
                        var futureShowtimes = m.Showtimes != null
                            ? m.Showtimes.Where(s => s.IsActive && s.StartTime >= realtimeNow).OrderBy(s => s.StartTime).ToList()
                            : null;

                        if (futureShowtimes != null && futureShowtimes.Any())
                        {
                            countValidMovies++;
                            string cleanTitle = m.Title.Replace("\"", "'").Replace("\n", " ").Replace("\r", " ");
                            string cleanGenre = (m.Genres != null && m.Genres.Any() ? string.Join(", ", m.Genres.Select(g => g.Name)) : "Phim").Replace("\"", "'");
                            string cleanDesc = !string.IsNullOrEmpty(m.Description) ? m.Description.Replace("\"", "'").Replace("\n", " ") : "Đang cập nhật nội dung.";
                            
                            // Giới hạn Description để tránh Context quá dài
                            if (cleanDesc.Length > 200) cleanDesc = cleanDesc.Substring(0, 200) + "...";

                            systemContext.AppendLine($"- PHIM: {cleanTitle} [{cleanGenre}]");
                            systemContext.AppendLine($"  Nội dung tóm tắt: {cleanDesc}");
                            systemContext.Append("  Các suất chiếu: ");
                            foreach (var st in futureShowtimes)
                            {
                                decimal normalPrice = st.BasePrice;
                                systemContext.Append($"[ID: {st.Id} | Giờ chiếu: {st.StartTime.ToString("HH:mm dd/MM")} | Định dạng: {st.Format} | Giá gốc: {normalPrice:N0}đ] ");
                            }
                            systemContext.AppendLine();
                        }
                    }
                }

                if (countValidMovies == 0) systemContext.AppendLine("- Hiện tại rạp chưa có suất chiếu nào sắp tới.");

                //  GỌI GEMINI API TRẢ LỜI CÂU HỎI
                string systemPrompt = $@"Bạn là trợ lý ảo nhiệt tình, sành điệu của rạp phim Cinema Hub. 
Nhiệm vụ: Tư vấn phim và hướng dẫn khách mua vé. 
Dữ liệu lịch chiếu: {systemContext}

LUẬT TRÌNH BÀY (BẮT BUỘC):
1. TRẢ LỜI BẰNG ĐỊNH DẠNG HTML (Tuyệt đối không dùng Markdown như **bold** hay bọc trong ```html...```).
2. Dùng thẻ <b> để in đậm. Dùng thẻ <br> để xuống dòng.
3. Nếu giới thiệu suất chiếu, BẮT BUỘC chèn 1 NÚT bấm HTML để khách mua vé với cấu trúc chính xác như sau:
   <a href='/Booking/SelectSeat?showtimeId=[ID_SUẤT_CHIẾU]' class='btn btn-sm btn-outline-success mt-1 mb-2' style='border-radius:20px; font-weight:600;'><i class='bi bi-ticket-perforated'></i> Đặt vé [GIỜ_CHIẾU]</a>
4. Trả lời cực kỳ ngắn gọn (2-3 câu), thân thiện, có emoji. Không bịa đặt lịch chiếu. Nếu khách hỏi phim không có, hãy xin lỗi và giới thiệu phim khác.

Câu hỏi của khách: {message}";

                var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                var requestBody = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = systemPrompt } } } }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    botReply = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    
                    // Xóa bỏ wrapper markdown nếu Gemini vẫn cố tình sinh ra
                    if (!string.IsNullOrEmpty(botReply))
                    {
                        botReply = botReply.Replace("```html", "").Replace("```", "").Trim();
                    }
                }

                // 4 CÁCH LY LUỒNG DATABASE (NẾU DB LỖI THÌ BỎ QUA, BOT VẪN TRẢ LỜI BÌNH THƯỜNG)
                try
                {
                    string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    ChatSession session = null;

                    if (sessionId.HasValue && sessionId.Value > 0)
                    {
                        session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId.Value);
                    }

                    if (session == null)
                    {
                        session = new ChatSession { UserId = currentUserId, StartedAt = DateTime.Now };
                        _context.ChatSessions.Add(session);
                        await _context.SaveChangesAsync();
                    }

                    if (session != null)
                    {
                        returnedSessionId = session.Id;
                        var userMsg = new ChatMessage { ChatSessionId = session.Id, Sender = "User", MessageText = message, SentAt = DateTime.Now };
                        var botMsg = new ChatMessage { ChatSessionId = session.Id, Sender = "Bot", MessageText = botReply, SentAt = DateTime.Now };

                        _context.ChatMessages.Add(userMsg);
                        _context.ChatMessages.Add(botMsg);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception dbEx)
                {
                }

                // KHÔNG BAO GIỜ TRẢ VỀ LỖI ĐỎ LÊN MÀN HÌNH NỮA, TRẢ VỀ CÂU NÓI CỦA BOT
                return Json(new { success = true, reply = botReply, sessionId = returnedSessionId });
            }
            catch (Exception ex)
            {
                // Lỗi cục bộ khi gọi API Google hoặc build dữ liệu phim
                return Json(new { success = true, reply = "Cinema Hub đang xử lý chút xíu, bạn vui lòng đợi xíu hoặc hỏi lại nha! 💕", sessionId = returnedSessionId });
            }
        }
    }
}