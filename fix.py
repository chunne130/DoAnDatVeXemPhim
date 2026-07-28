import sys

file_path = r'd:\DoAnDatVeXemPhim\Views\AdminControllers\ManageOrders.cshtml'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix the thead
old_thead = '''                    <th style="padding-bottom:12px;">Khách hàng & Liên hệ</th>
                    <th class="text-center" style="padding-bottom:12px;">Mã GD (Bank)</th>
                    <th class="text-center" style="padding-bottom:12px;">Thời gian đặt</th>
                    <th class="text-end" style="padding-bottom:12px;">Tổng tiền</th>
                    <th class="text-center" style="padding-bottom:12px;">Trạng thái</th>
                    <th class="text-center" style="width:200px;padding-bottom:12px;">Thao tác</th>'''

new_thead = '''                    <th style="padding-bottom:12px;">Khách hàng & Liên hệ</th>
                    <th class="text-center" style="padding-bottom:12px;">Mã GD (Bank)</th>
                    <th class="text-center" style="padding-bottom:12px;">Thời gian đặt</th>
                    <th class="text-end" style="padding-bottom:12px;">Tổng tiền</th>
                    <th class="text-center" style="padding-bottom:12px;">Thanh toán</th>
                    <th class="text-center" style="padding-bottom:12px;">Tình trạng vé</th>
                    <th class="text-center" style="width:200px;padding-bottom:12px;">Thao tác</th>'''

content = content.replace(old_thead, new_thead)

# Fix the tbody body
old_tbody_error = '''                                    <div style="font-size:0.75rem;color:#9ca3af;"><i class="bi bi-telephone me-1"></i>@(item.User.PhoneNumber ?? "Chưa có SĐT")</div>
                                    </button>'''

new_tbody_fix = '''                                    <div style="font-size:0.75rem;color:#9ca3af;"><i class="bi bi-telephone me-1"></i>@(item.User.PhoneNumber ?? "Chưa có SĐT")</div>
                                }
                                else
                                {
                                    <span class="text-muted fst-italic">Khách ẩn danh</span>
                                }
                            </td>
                            <td class="text-center">
                                <code class="fw-bold px-2 py-1 rounded"
                                      style="background:rgba(96,165,250,0.1);color:#60a5fa;border:1px dashed rgba(96,165,250,0.3);">
                                    CHUB@(item.Id)@item.OrderDate.ToString("HHmm")
                                </code>
                            </td>
                            <td class="text-center">
                                <div class="fw-bold text-light" style="font-size:0.85rem;">@item.OrderDate.ToString("dd/MM/yyyy")</div>
                                <div class="text-muted" style="font-size:0.75rem;">@item.OrderDate.ToString("HH:mm")</div>
                            </td>
                            <td class="text-end">
                                <span class="fw-bold fs-6" style="color:#00ff87;text-shadow:0 0 8px rgba(0,255,135,0.3);">
                                    @item.TotalAmount.ToString("N0")<span style="font-size:0.7rem;color:#9ca3af;margin-left:2px;">đ</span>
                                </span>
                            </td>
                            <td class="text-center">
                                @switch (item.Status)
                                {
                                    case "PAID":
                                        <span class="badge rounded-pill px-3 py-1"
                                              style="background:rgba(0,255,135,0.15);color:#00ff87;border:1px solid rgba(0,255,135,0.3);">
                                            <i class="bi bi-check2-circle me-1"></i> Đã thanh toán
                                        </span>
                                        break;
                                    case "CHECKED_IN":
                                        <span class="badge rounded-pill px-3 py-1"
                                              style="background:rgba(59,130,246,0.15);color:#60a5fa;border:1px solid rgba(59,130,246,0.3);">
                                            <i class="bi bi-qr-code-scan me-1"></i> Đã soát vé
                                        </span>
                                        break;
                                    case "CANCELLED":
                                        <span class="badge rounded-pill px-3 py-1"
                                              style="background:rgba(239,68,68,0.15);color:#ef4444;border:1px solid rgba(239,68,68,0.3);">
                                            <i class="bi bi-x-circle me-1"></i> Đã hủy
                                        </span>
                                        break;
                                    default:
                                             <span class="badge rounded-pill px-3 py-1 animate-pulse"
                                                   style="background:rgba(251,191,36,0.15);color:#fbbf24;border:1px solid rgba(251,191,36,0.3);">
                                                 <i class="bi bi-hourglass-split me-1"></i> Chờ xác nhận
                                             </span>
                                        break;
                                }
                            </td>
                            <td class="text-center">
                                @{
                                    var detail = item.OrderDetails?.FirstOrDefault();
                                    bool isPast = detail != null && detail.Showtime.StartTime < DateTime.Now;
                                }
                                @if (item.Status == "CHECKED_IN")
                                {
                                    <span class="badge bg-secondary"><i class="bi bi-check-all"></i> Đã sử dụng</span>
                                }
                                else if (item.Status == "CANCELLED")
                                {
                                    <span class="badge bg-danger"><i class="bi bi-x"></i> Vé hủy</span>
                                }
                                else if (isPast)
                                {
                                    <span class="badge bg-dark text-muted"><i class="bi bi-clock-history"></i> Hết hạn</span>
                                }
                                else if (item.IsPaid)
                                {
                                    <span class="badge bg-success"><i class="bi bi-ticket-perforated"></i> Hợp lệ</span>
                                }
                                else
                                {
                                    <span class="badge bg-warning text-dark"><i class="bi bi-exclamation-circle"></i> Chưa thanh toán</span>
                                }
                            </td>
                            <td>
                                <div class="d-flex gap-2 justify-content-center">
                                    <button type="button"
                                            class="btn btn-sm border-0 d-flex align-items-center justify-content-center btn-trigger-modal"
                                            style="background:rgba(96,165,250,0.1);color:#60a5fa;width:32px;height:32px;border-radius:6px;"
                                            data-order-id="@item.Id" title="Chi tiết">
                                        <i class="bi bi-eye"></i>
                                    </button>'''

content = content.replace(old_tbody_error, new_tbody_fix)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Fixed!")
