using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfSigning.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignedArtifactStorageKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignedArtifactStorageKey",
                table: "Documents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedArtifactStorageKey",
                table: "Documents");
        }
    }
}
