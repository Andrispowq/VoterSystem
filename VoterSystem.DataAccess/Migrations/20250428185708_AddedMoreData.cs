using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoterSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedMoreData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoteChoice_Votings_VotingId",
                table: "VoteChoice");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_VoteChoice_VoteChoiceChoiceId",
                table: "Votes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VoteChoice",
                table: "VoteChoice");

            migrationBuilder.RenameTable(
                name: "VoteChoice",
                newName: "VoteChoices");

            migrationBuilder.RenameIndex(
                name: "IX_VoteChoice_VotingId_Name",
                table: "VoteChoices",
                newName: "IX_VoteChoices_VotingId_Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VoteChoices",
                table: "VoteChoices",
                column: "ChoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoteChoices_Votings_VotingId",
                table: "VoteChoices",
                column: "VotingId",
                principalTable: "Votings",
                principalColumn: "VotingId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_VoteChoices_VoteChoiceChoiceId",
                table: "Votes",
                column: "VoteChoiceChoiceId",
                principalTable: "VoteChoices",
                principalColumn: "ChoiceId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoteChoices_Votings_VotingId",
                table: "VoteChoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_VoteChoices_VoteChoiceChoiceId",
                table: "Votes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VoteChoices",
                table: "VoteChoices");

            migrationBuilder.RenameTable(
                name: "VoteChoices",
                newName: "VoteChoice");

            migrationBuilder.RenameIndex(
                name: "IX_VoteChoices_VotingId_Name",
                table: "VoteChoice",
                newName: "IX_VoteChoice_VotingId_Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VoteChoice",
                table: "VoteChoice",
                column: "ChoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoteChoice_Votings_VotingId",
                table: "VoteChoice",
                column: "VotingId",
                principalTable: "Votings",
                principalColumn: "VotingId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_VoteChoice_VoteChoiceChoiceId",
                table: "Votes",
                column: "VoteChoiceChoiceId",
                principalTable: "VoteChoice",
                principalColumn: "ChoiceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
