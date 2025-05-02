using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoterSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixedChoiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Votes_VoteChoices_VoteChoiceChoiceId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Votes_VoteChoiceChoiceId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "VoteChoiceChoiceId",
                table: "Votes");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ChoiceId",
                table: "Votes",
                column: "ChoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_VoteChoices_ChoiceId",
                table: "Votes",
                column: "ChoiceId",
                principalTable: "VoteChoices",
                principalColumn: "ChoiceId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Votes_VoteChoices_ChoiceId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Votes_ChoiceId",
                table: "Votes");

            migrationBuilder.AddColumn<long>(
                name: "VoteChoiceChoiceId",
                table: "Votes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_VoteChoiceChoiceId",
                table: "Votes",
                column: "VoteChoiceChoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_VoteChoices_VoteChoiceChoiceId",
                table: "Votes",
                column: "VoteChoiceChoiceId",
                principalTable: "VoteChoices",
                principalColumn: "ChoiceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
