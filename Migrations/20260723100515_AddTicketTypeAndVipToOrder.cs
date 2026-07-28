using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnDatVeXemPhim.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTypeAndVipToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VipDiscount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VipLevel",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketType",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VipDiscount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VipLevel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TicketType",
                table: "OrderDetails");
        }
    }
}
