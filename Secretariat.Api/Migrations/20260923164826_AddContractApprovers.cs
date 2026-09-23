using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Secretariat.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContractApprovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractApprovers_AppUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractApprovers_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractApprovers_ApproverUserId",
                table: "ContractApprovers",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractApprovers_ContractId_ApproverUserId",
                table: "ContractApprovers",
                columns: new[] { "ContractId", "ApproverUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractApprovers_ContractId_Role",
                table: "ContractApprovers",
                columns: new[] { "ContractId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractApprovers");
        }
    }
}
