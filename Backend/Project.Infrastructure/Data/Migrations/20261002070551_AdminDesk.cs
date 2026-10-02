using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminDesk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssigneeId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Decision",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "Reports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedById",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssigneeId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Decision",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "Conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedById",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrustLevel",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ConversationReads",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationReads", x => new { x.ConversationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ConversationReads_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AssigneeId",
                table: "Reports",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ResolvedById",
                table: "Reports",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AssigneeId",
                table: "Conversations",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ResolvedById",
                table: "Conversations",
                column: "ResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_AssigneeId",
                table: "Conversations",
                column: "AssigneeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_ResolvedById",
                table: "Conversations",
                column: "ResolvedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_AssigneeId",
                table: "Reports",
                column: "AssigneeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedById",
                table: "Reports",
                column: "ResolvedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_AssigneeId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_ResolvedById",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_AssigneeId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ResolvedById",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "ConversationReads");

            migrationBuilder.DropIndex(
                name: "IX_Reports_AssigneeId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ResolvedById",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_AssigneeId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ResolvedById",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Decision",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ResolvedById",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Decision",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ReportId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ResolvedById",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TrustLevel",
                table: "AspNetUsers");
        }
    }
}
