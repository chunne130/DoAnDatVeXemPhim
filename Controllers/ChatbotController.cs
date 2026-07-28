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
                systemContext.AppendLine("QUY ĐỊNH RẠP: Giá vé gốc dành cho Ghế Thường. Ghế VIP phụ thu 20.000đ. Giảm 10% cho HSSV.");
                systemContext.AppendLine($"HIỆN TẠI: {realtimeNow.ToString("HH:mm dd/MM/yyyy")}");
                systemContext.AppendLine("LỊCH CHIẾU THỰC TẾ:");

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

                            systemContext.Append($"- Phim: {cleanTitle} [{cleanGenre}]. Suất: ");
                            foreach (var st in futureShowtimes)
                            {
                                decimal normalPrice = st.BasePrice;
                                decimal vipPrice = st.BasePrice + 20000;

                                var bookedSeats = await _context.OrderDetails
                                    .Include(od => od.Order)
                                    .Where(od => od.ShowtimeId == st.Id && (od.Order.IsPaid == true || (od.Order.IsPaid == false && st.StartTime > realtimeNow)))
                                    .Select(od => od.Seat.SeatNumber)
                                    .ToListAsync();

                                string bookedSeatsStr = bookedSeats.Any() ? string.Join(", ", bookedSeats) : "Chưa có";
                                systemContext.Append($"[{st.StartTime.ToString("HH:mm dd/MM")} - {st.Format} - Thường: {normalPrice:N0}đ, VIP: {vipPrice:N0}đ - Ghế đã bán: {bookedSeatsStr}] ");
                            }
                            systemContext.AppendLine();
                        }
                    }
                }

                if (countValidMovies == 0) systemContext.AppendLine("- Hiện tại chưa có suất chiếu nào.");

                //  GỌI GEMINI API TRẢ LỜI CÂU HỎI
                var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                var requestBody = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = $"Bạn là trợ lý bán vé Cinema Hub. Hãy tư vấn ngắn gọn (2-3 câu). Trả lời dựa trên lịch chiếu dưới đây.\n\n[DỮ LIỆU]:\n{systemContext}\n\nCâu hỏi: {message}" } } } }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    botReply = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
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