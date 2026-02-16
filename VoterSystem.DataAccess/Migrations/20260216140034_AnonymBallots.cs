using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VoterSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AnonymBallots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.AddColumn<byte[]>(
                name: "KeySalt",
                table: "Votings",
                type: "bytea",
                maxLength: 32,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "VoteCount",
                table: "VoteChoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnonymousBallots",
                columns: table => new
                {
                    AnonymousBallotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VotingId = table.Column<long>(type: "bigint", nullable: false),
                    ChoiceId = table.Column<long>(type: "bigint", nullable: false),
                    VoteTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousBallots", x => x.AnonymousBallotId);
                    table.ForeignKey(
                        name: "FK_AnonymousBallots_VoteChoices_ChoiceId",
                        column: x => x.ChoiceId,
                        principalTable: "VoteChoices",
                        principalColumn: "ChoiceId");
                    table.ForeignKey(
                        name: "FK_AnonymousBallots_Votings_VotingId",
                        column: x => x.VotingId,
                        principalTable: "Votings",
                        principalColumn: "VotingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotingParticipations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotingId = table.Column<long>(type: "bigint", nullable: false),
                    HasVoted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingParticipations", x => new { x.UserId, x.VotingId });
                    table.ForeignKey(
                        name: "FK_VotingParticipations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VotingParticipations_Votings_VotingId",
                        column: x => x.VotingId,
                        principalTable: "Votings",
                        principalColumn: "VotingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousBallots_ChoiceId",
                table: "AnonymousBallots",
                column: "ChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousBallots_VotingId_ChoiceId",
                table: "AnonymousBallots",
                columns: new[] { "VotingId", "ChoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousBallots_VotingId_VoteTag",
                table: "AnonymousBallots",
                columns: new[] { "VotingId", "VoteTag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingParticipations_VotingId",
                table: "VotingParticipations",
                column: "VotingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonymousBallots");

            migrationBuilder.DropTable(
                name: "VotingParticipations");

            migrationBuilder.DropColumn(
                name: "KeySalt",
                table: "Votings");

            migrationBuilder.DropColumn(
                name: "VoteCount",
                table: "VoteChoices");

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChoiceId = table.Column<long>(type: "bigint", nullable: false),
                    VotingId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => new { x.UserId, x.ChoiceId });
                    table.ForeignKey(
                        name: "FK_Votes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Votes_VoteChoices_ChoiceId",
                        column: x => x.ChoiceId,
                        principalTable: "VoteChoices",
                        principalColumn: "ChoiceId");
                    table.ForeignKey(
                        name: "FK_Votes_Votings_VotingId",
                        column: x => x.VotingId,
                        principalTable: "Votings",
                        principalColumn: "VotingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ChoiceId",
                table: "Votes",
                column: "ChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_VotingId",
                table: "Votes",
                column: "VotingId");
        }
    }
}
