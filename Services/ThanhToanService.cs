using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DoAnDatVeXemPhim.Services
{
    public class ThanhToanService
    {
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _baseUrl;

        public ThanhToanService(IConfiguration configuration)
        {
            // Lấy các thông số cấu hình từ file appsettings.json
            _clientId = configuration["PayOS:ClientId"];
            _apiKey = configuration["PayOS:ApiKey"];
            _checksumKey = configuration["PayOS:ChecksumKey"];
            // Nếu không có BaseUrl trong cấu hình thì dùng localhost mặc định
            _baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:13015";
        }

        /// <summary>
        /// Hàm chính để tạo Link thanh toán QR Code từ PayOS
        /// </summary>
        public async Task<string> CreatePaymentLink(int orderId, decimal amount)
        {
            try
            {
                // BƯỚC 1: CHUẨN BỊ DỮ LIỆU ĐƠN HÀNG
                // Tạo orderCode duy nhất bằng thời gian (Ví dụ: Ngày 30 lúc 18h30'15s -> 30183015)
                long orderCode = long.Parse(DateTime.Now.ToString("ddHHmmss"));
                int intAmount = (int)amount; // PayOS yêu cầu số tiền kiểu số nguyên (int)

                // Nội dung chuyển khoản (Lưu ý: Không nên có dấu hoặc ký tự đặc biệt để tránh lỗi)
                string description = "Thanh toan don hang " + orderId;

                // Đường dẫn trả về khi khách bấm "Hủy" hoặc "Thành công" trên trang QR
                string cancelUrl = $"{_baseUrl}/Booking/Checkout";
                string returnUrl = $"{_baseUrl}/Booking/PaymentSuccess?orderId={orderId}";

                // BƯỚC 2: TẠO CHỮ KÝ BẢO MẬT (SIGNATURE)
                // Quan trọng: Các trường dữ liệu PHẢI sắp xếp theo thứ tự bảng chữ cái (A-Z)
                // Thứ tự: amount -> cancelUrl -> description -> orderCode -> returnUrl
                string rawData = $"amount={intAmount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";

                // Dùng ChecksumKey để băm chuỗi dữ liệu thành mã SHA256 (đảm bảo dữ liệu không bị sửa đổi)
                string signature = CalculateHmacSha256(rawData, _checksumKey.Trim());

                // BƯỚC 3: ĐÓNG GÓI JSON GỬI ĐI
                var paymentData = new
                {
                    orderCode = orderCode,
                    amount = intAmount,
                    description = description,
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    signature = signature
                };

                // BƯỚC 4: GỌI API PAYOS
                using var client = new HttpClient();
                // Thêm Client ID và API Key vào Header để PayOS xác thực tài khoản
                client.DefaultRequestHeaders.Add("x-client-id", _clientId.Trim());
                client.DefaultRequestHeaders.Add("x-api-key", _apiKey.Trim());

                var json = JsonConvert.SerializeObject(paymentData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gửi yêu cầu tạo link thanh toán tới server PayOS
                var response = await client.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // BƯỚC 5: XỬ LÝ KẾT QUẢ TRẢ VỀ
                var result = JObject.Parse(responseContent);

                // PayOS trả về trường "code" (00 là thành công, khác là lỗi)
                string code = result["code"]?.ToString();

                if (code == "0" || code == "00")
                {
                    // Lấy đường dẫn link QR (checkoutUrl) để Redirect khách sang thanh toán
                    return result["data"]?["checkoutUrl"]?.ToString();
                }
                else
                {
                    // Nếu PayOS báo lỗi (Ví dụ: sai chữ ký, số tiền không hợp lệ...)
                    string message = result["desc"]?.ToString() ?? result["message"]?.ToString();
                    throw new Exception($"PayOS Error [{code}]: {message}");
                }
            }
            catch (Exception ex)
            {
                // Bắt các lỗi kết nối mạng hoặc lỗi code bên trong
                throw new Exception("Lỗi hệ thống khi tạo link: " + ex.Message);
            }
        }

        /// <summary>
        /// Hàm mã hóa dữ liệu theo chuẩn HMAC-SHA256
        /// Giúp tạo ra một chuỗi bảo mật mà chỉ mình và PayOS mới biết cách kiểm tra
        /// </summary>
        private string CalculateHmacSha256(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);
                // Chuyển mảng byte thành chuỗi Hex (viết thường, không có dấu gạch ngang)
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}