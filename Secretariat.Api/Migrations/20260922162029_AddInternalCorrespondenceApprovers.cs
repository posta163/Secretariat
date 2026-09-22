using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Secretariat.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalCorrespondenceApprovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternalCorrespondenceApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InternalCorrespondenceId = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalCorrespondenceApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalCorrespondenceApprovers_AppUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalCorrespondenceApprovers_InternalCorrespondences_InternalCorrespondenceId",
                        column: x => x.InternalCorrespondenceId,
                        principalTable: "InternalCorrespondences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternalCorrespondenceApprovers_ApproverUserId",
                table: "InternalCorrespondenceApprovers",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalCorrespondenceApprovers_InternalCorrespondenceId_ApproverUserId",
                table: "InternalCorrespondenceApprovers",
                columns: new[] { "InternalCorrespondenceId", "ApproverUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternalCorrespondenceApprovers");
        }
    }
}
