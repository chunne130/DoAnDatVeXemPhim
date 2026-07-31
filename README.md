# 🎬 Cinema Hub - Movie Ticket Booking Platform

![Platform](https://img.shields.io/badge/Platform-Web-000000.svg?style=for-the-badge&logo=google-chrome)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120.svg?style=for-the-badge&logo=c-sharp)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)

**Cinema Hub** is a modern, full-stack web application built to streamline movie ticket bookings. The platform ensures a seamless user experience with real-time seat locking, secure online payments, and instant e-ticket generation, making cinema management highly efficient.

> **Status:** 🚀 Completed / Ready for deployment

---

## 💻 Screenshots

<!-- HƯỚNG DẪN THÊM ẢNH: 
1. Bạn tạo một thư mục tên là "Captures" ngay trong repo Github của dự án.
2. Chụp ảnh màn hình web của bạn, đổi tên tương ứng (home.png, booking.png...) rồi up vào thư mục đó.
3. Nếu bạn đặt tên khác, chỉ cần sửa lại chữ "Captures/tên_ảnh_của_bạn.png" ở các thẻ <img> bên dưới.
-->

| Home Page | Movie Details | Seat Booking |
|:---:|:---:|:---:|
| <img src="Captures/home.png" width="300"/> | <img src="Captures/movie_details.png" width="300"/> | <img src="Captures/booking.png" width="300"/> |

| Payment (PayOS) | E-Ticket (QR Code) | Admin Dashboard |
|:---:|:---:|:---:|
| <img src="Captures/payment.png" width="300"/> | <img src="Captures/ticket.png" width="300"/> | <img src="Captures/admin.png" width="300"/> |

---

## ✨ Key Features

### 🎫 Real-Time Seat Reservation
- **Concurrency Control:** Utilizes `SignalR` to synchronize seat locking in real-time across all active users.
- **Double-booking Prevention:** Ensures that once a seat is selected by a user, it becomes instantly unavailable to others until checkout is complete or timed out.

### 💳 Secure Payment Workflow
- **PayOS Integration:** Seamlessly processes online transactions using the PayOS payment gateway.
- **Automated E-Tickets:** Generates digital tickets with scannable QR codes (via `QRCoder`) immediately upon successful payment.

### 🔐 Authentication & Security
- **Role-Based Access Control (RBAC):** Distinct interfaces and permissions for `Admin` (managing movies, shows, and users) and `User` (booking tickets).
- **Google OAuth:** Fast and secure social login alongside standard `ASP.NET Core Identity` authentication.

### 🗄️ Robust Backend Architecture
- **Entity Framework Core:** Optimized relational database design utilizing PostgreSQL.
- **Docker Ready:** Containerized setup for easy deployment and testing across different environments.

---

## 🛠️ Tech Stack & Tools

- **Backend:** C#, ASP.NET Core 8 MVC, Entity Framework Core
- **Database:** PostgreSQL
- **Real-time Engine:** SignalR
- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap
- **External Services:** PayOS API, Google OAuth API
- **Utilities:** QRCoder, Docker

---
## 🚀 How to Run Locally

1. **Clone the repository:**

    ```bash
    git clone https://github.com/chunne130/DoAnDatVeXemPhim.git
    cd DoAnDatVeXemPhim
    ```

2. **Setup Database & Run Application:**

    Update the Connection String in `appsettings.json` with your local PostgreSQL credentials, then execute:

    ```bash
    dotnet ef database update
    dotnet run
    ```

    *The application will be available at `https://localhost:5001` (or the port specified in your launch settings).*
