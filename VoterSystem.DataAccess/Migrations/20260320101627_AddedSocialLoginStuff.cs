using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoterSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedSocialLoginStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoginMode",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginMode",
                table: "AspNetUsers");
        }
    }
}
