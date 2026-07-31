# 🎬 Cinema Hub - Movie Ticket Booking Platform

![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.Net](https://img.shields.io/badge/.NET_Core_8-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)
![Bootstrap](https://img.shields.io/badge/bootstrap-%238511FA.svg?style=for-the-badge&logo=bootstrap&logoColor=white)
![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)

> A modern, full-stack movie ticket booking platform built with ASP.NET Core 8 MVC. This project features real-time seat booking, secure authentication, online payment integration, and a responsive Cyberpunk-themed user interface.

*(Bạn có thể chèn 1-2 ảnh chụp màn hình website thật đẹp của bạn vào đây)*

## ✨ Key Features

- **Real-time Seat Booking:** Utilizes **SignalR** to lock seats in real-time, preventing booking conflicts when multiple users are selecting seats simultaneously.
- **Secure Authentication:** Role-based access control (Admin/User) integrated with **ASP.NET Core Identity** and **Google OAuth**.
- **Online Payment:** Seamless payment gateway integration via **PayOS**.
- **E-Tickets (QR Code):** Automatic ticket generation with scannable QR codes using **QRCoder**.
- **Responsive UI/UX:** Dark-theme Cyberpunk aesthetic designed with HTML5, CSS3, JavaScript, and Bootstrap.
- **Dockerized:** Ready for deployment with multi-stage Docker builds.

## 🛠️ Tech Stack

- **Backend:** C#, ASP.NET Core 8 MVC, Entity Framework Core, SignalR, Identity
- **Database:** PostgreSQL / SQL Server
- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5
- **Infrastructure & Tools:** Docker, Git, PayOS API, QRCoder

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL / SQL Server
- Docker (Optional, for containerized deployment)

### Installation

1. Clone the repository
   ```bash
   git clone https://github.com/chunne130/DoAnDatVeXemPhim.git
   cd DoAnDatVeXemPhim
   ```

2. Update Database Connection
   Open `appsettings.json` and update the `DefaultConnection` string with your database credentials.

3. Apply Entity Framework Migrations
   ```bash
   dotnet ef database update
   ```

4. Run the application
   ```bash
   dotnet run
   ```

## 🐳 Docker Deployment

To run the application using Docker:
```bash
docker build -t cinemahub .
docker run -d -p 8080:8080 --name cinemahub-app cinemahub
```
Navigate to `http://localhost:8080` to view the app.

---
*This is an academic/personal project developed for learning and demonstrating full-stack development skills.*
