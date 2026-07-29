using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DoAnDatVeXemPhim.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // THÔNG TIN BREVO API (Thay thế hoàn toàn SMTP bị Render chặn)
            var fromEmail = "nguyenthanhho2005@gmail.com"; // Email đã verify trên Brevo
            var fromName = "Cinema Hub Support";
            var apiKey = "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";

            // Chuẩn bị payload JSON theo chuẩn API v3 của Brevo
            var payload = new
            {
                sender = new { name = fromName, email = fromEmail },
                to = new[] { new { email = email } },
                subject = subject,
                htmlContent = htmlMessage
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                // Gắn API Key vào Header (Giao tiếp qua cổng 443 HTTPS chống chặn)
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.DefaultRequestHeaders.Add("accept", "application/json");

                try
                {
                    // Gửi request POST tới máy chủ Brevo
                    var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("=== LỖI BREVO API: " + errorResponse);
                        throw new System.Exception($"Lỗi gọi Brevo API: {response.StatusCode} - {errorResponse}");
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("=== LỖI GỬI MAIL BREVO: " + ex.Message);
                    throw new System.Exception("Lỗi hệ thống gửi Mail (Brevo API): " + ex.Message);
                }
            }
        }
    }
}