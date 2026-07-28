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

            using (var client = new SmtpClient("smtp.gmail.com", 587))
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