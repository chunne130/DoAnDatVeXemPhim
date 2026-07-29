using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace DoAnDatVeXemPhim.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // THÔNG TIN GMAIL 
            var fromEmail = "nguyenthanhho2005@gmail.com";

            // MẬT KHẨU ỨNG DỤNG
            var fromPassword = "mijwgtnfdgpfddao";

            // FIX LỖI GỬI MAIL TRÊN RENDER: 
            // Render ưu tiên dùng IPv6, nhưng Gmail sẽ tự động chặn mọi email gửi từ IPv6 nếu không có Reverse DNS (PTR).
            // Do đó ta phải ép hệ thống dùng IPv4 của Gmail và bỏ qua lỗi xác thực tên miền chứng chỉ (do dùng thẳng IP).
            string smtpHost = "smtp.gmail.com";
            try
            {
                var addresses = Dns.GetHostAddresses("smtp.gmail.com");
                var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipv4 != null) smtpHost = ipv4.ToString();
            }
            catch { }

            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new SmtpClient(smtpHost, 587))
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(fromEmail, fromPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Cinema Hub Support"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                try
                {
                    // Thực hiện gửi mail thực tế
                    await client.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    // Ép lỗi hiện ra cửa sổ Output để có lỗi dễ báo
                    System.Diagnostics.Debug.WriteLine("=== LỖI GỬI MAIL THIỆT NÈ: " + ex.Message);

                    // Nếu lỗi SMTP, nó sẽ quăng lỗi ra trình duyệt để biết 
                    throw new Exception("Lỗi hệ thống không gửi được Email: " + ex.Message);
                }
            }
        }
    }
}