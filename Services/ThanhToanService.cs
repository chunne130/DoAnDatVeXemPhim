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

        // VNPAY Cấu hình
        private readonly string _vnpTmnCode;
        private readonly string _vnpHashSecret;
        private readonly string _vnpUrl;

        public ThanhToanService(IConfiguration configuration)
        {
            // Lấy các thông số cấu hình từ file appsettings.json
            _clientId = configuration["PayOS:ClientId"];
            _apiKey = configuration["PayOS:ApiKey"];
            _checksumKey = configuration["PayOS:ChecksumKey"];
            
            _vnpTmnCode = configuration["VNPay:TmnCode"];
            _vnpHashSecret = configuration["VNPay:HashSecret"];
            _vnpUrl = configuration["VNPay:BaseUrl"];

            _baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:13015";
        }

        /// <summary>
        /// Hàm chính để tạo Link thanh toán QR Code từ PayOS, trả về cả URL và OrderCode
        /// </summary>
        public async Task<(string checkoutUrl, long orderCode)> CreatePaymentLink(int orderId, decimal amount, string customCancelUrl = null)
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
                string cancelUrl = customCancelUrl ?? $"{_baseUrl}/Booking/Checkout";
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
                    string checkoutUrl = result["data"]?["checkoutUrl"]?.ToString();
                    return (checkoutUrl, orderCode);
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
        /// Gọi API PayOS để lấy thông tin thanh toán thực tế của đơn hàng (Chống gian lận)
        /// </summary>
        public async Task<JToken> GetPaymentInfo(long orderCode)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-client-id", _clientId.Trim());
                client.DefaultRequestHeaders.Add("x-api-key", _apiKey.Trim());

                var response = await client.GetAsync($"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}");
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JObject.Parse(responseContent);
                string code = result["code"]?.ToString();

                if (code == "0" || code == "00")
                {
                    return result["data"];
                }
                
                return null;
            }
            catch (Exception)
            {
                return null;
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

        /// <summary>
        /// Tạo URL chuyển hướng thanh toán VNPAY
        /// </summary>
        public string CreateVnPayPaymentUrl(int orderId, decimal amount, HttpContext context, string customReturnUrl = null)
        {
            var vnpay = new VnPayLibrary();
            long vnpAmount = (long)(amount * 100); // VNPAY yêu cầu nhân 100 (bỏ thập phân)
            
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _vnpTmnCode);
            vnpay.AddRequestData("vnp_Amount", vnpAmount.ToString());
            
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + orderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            
            string returnUrl = customReturnUrl ?? $"{_baseUrl}/Booking/VnPayReturn";
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            
            // Generate unique txn ref based on orderId and time to avoid duplicates
            vnpay.AddRequestData("vnp_TxnRef", $"{orderId}_{DateTime.Now.Ticks}");

            string paymentUrl = vnpay.CreateRequestUrl(_vnpUrl, _vnpHashSecret);
            return paymentUrl;
        }
    }
}