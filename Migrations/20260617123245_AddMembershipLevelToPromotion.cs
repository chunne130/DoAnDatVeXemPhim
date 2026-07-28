using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnDatVeXemPhim.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipLevelToPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MembershipLevelId",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_MembershipLevelId",
                table: "Promotions",
                column: "MembershipLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_MembershipLevels_MembershipLevelId",
                table: "Promotions",
                column: "MembershipLevelId",
                principalTable: "MembershipLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_MembershipLevels_MembershipLevelId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_MembershipLevelId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MembershipLevelId",
                table: "Promotions");
        }
    }
}
