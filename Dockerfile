# 1. Build Phase (Dùng SDK để biên dịch code)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy file csproj và tải các thư viện nuget
COPY ["DoAnDatVeXemPhim.csproj", "./"]
RUN dotnet restore "DoAnDatVeXemPhim.csproj"

# Copy toàn bộ code còn lại và Publish ra file chạy (.dll)
COPY . .
RUN dotnet publish "DoAnDatVeXemPhim.csproj" -c Release -o /app/publish

# 2. Runtime Phase (Chỉ dùng bản Runtime nhẹ nhàng để chạy Web)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# Copy những file đã build xong từ Phase 1 sang đây
COPY --from=build /app/publish .

# Định nghĩa cổng chạy và lệnh khởi động
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DoAnDatVeXemPhim.dll"]