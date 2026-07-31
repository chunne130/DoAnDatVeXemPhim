using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnDatVeXemPhim.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderToSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Seats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Khởi tạo DisplayOrder cho các ghế cũ (ví dụ C10 -> 10)
            migrationBuilder.Sql("UPDATE \"Seats\" SET \"DisplayOrder\" = CAST(SUBSTRING(\"SeatNumber\" FROM 2) AS INTEGER) WHERE \"SeatNumber\" ~ '^[A-Z][0-9]+$';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Seats");
        }
    }
}
