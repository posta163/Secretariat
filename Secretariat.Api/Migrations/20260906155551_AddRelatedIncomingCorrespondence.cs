using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Secretariat.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedIncomingCorrespondence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatedIncomingCorrespondenceId",
                table: "Correspondences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_RelatedIncomingCorrespondenceId",
                table: "Correspondences",
                column: "RelatedIncomingCorrespondenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Correspondences_Correspondences_RelatedIncomingCorrespondenceId",
                table: "Correspondences",
                column: "RelatedIncomingCorrespondenceId",
                principalTable: "Correspondences",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Correspondences_Correspondences_RelatedIncomingCorrespondenceId",
                table: "Correspondences");

            migrationBuilder.DropIndex(
                name: "IX_Correspondences_RelatedIncomingCorrespondenceId",
                table: "Correspondences");

            migrationBuilder.DropColumn(
                name: "RelatedIncomingCorrespondenceId",
                table: "Correspondences");
        }
    }
}
