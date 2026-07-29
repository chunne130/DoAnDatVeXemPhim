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
            // THÔNG TIN GOOGLE APPS SCRIPT API (Lách luật Render SMTP)
            // Giao tiếp qua cổng 443 HTTPS không bao giờ bị chặn
            var scriptUrl = "https://script.google.com/macros/s/AKfycbwCOgjt0Vk68xjjfVGZlcoo9E6_ezGHlMjjizIUNRyUKA4VQTrlpL86vf9Ify_qOspM-A/exec";

            // Chuẩn bị payload JSON theo chuẩn mà ta đã viết trên Google Script
            var payload = new
            {
                to = email,
                subject = subject,
                htmlBody = htmlMessage
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                try
                {
                    // Gửi request POST tới máy chủ Google
                    var response = await client.PostAsync(scriptUrl, content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("=== LỖI GOOGLE SCRIPT API: " + errorResponse);
                        throw new System.Exception($"Lỗi gọi Google API: {response.StatusCode} - {errorResponse}");
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("=== LỖI GỬI MAIL QUA GOOGLE SCRIPT: " + ex.Message);
                    throw new System.Exception("Lỗi hệ thống gửi Mail (Google Script): " + ex.Message);
                }
            }
        }
    }
}