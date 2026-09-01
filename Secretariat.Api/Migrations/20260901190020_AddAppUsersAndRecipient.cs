using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Secretariat.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUsersAndRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipientUserId",
                table: "Correspondences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntraObjectId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_RecipientUserId",
                table: "Correspondences",
                column: "RecipientUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Correspondences_AppUsers_RecipientUserId",
                table: "Correspondences",
                column: "RecipientUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Correspondences_AppUsers_RecipientUserId",
                table: "Correspondences");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_Correspondences_RecipientUserId",
                table: "Correspondences");

            migrationBuilder.DropColumn(
                name: "RecipientUserId",
                table: "Correspondences");
        }
    }
}
