using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnDatVeXemPhim.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdminHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdminHandling",
                table: "ChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdminHandling",
                table: "ChatSessions");
        }
    }
}
