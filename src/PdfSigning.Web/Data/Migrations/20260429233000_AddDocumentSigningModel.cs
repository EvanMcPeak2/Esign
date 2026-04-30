using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfSigning.Web.Data.Migrations;

public partial class AddDocumentSigningModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documents", x => x.Id);
                table.ForeignKey(
                    name: "FK_Documents_AspNetUsers_OwnerUserId",
                    column: x => x.OwnerUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SigningSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                AccessTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SigningSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SigningSessions_Documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SignatureFields",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SigningSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PageNumber = table.Column<int>(type: "int", nullable: false),
                X = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Y = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Width = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Height = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                IsRequired = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SignatureFields", x => x.Id);
                table.ForeignKey(
                    name: "FK_SignatureFields_Documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.NoAction);
                table.ForeignKey(
                    name: "FK_SignatureFields_SigningSessions_SigningSessionId",
                    column: x => x.SigningSessionId,
                    principalTable: "SigningSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Documents_OwnerUserId",
            table: "Documents",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SignatureFields_DocumentId",
            table: "SignatureFields",
            column: "DocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_SignatureFields_SigningSessionId",
            table: "SignatureFields",
            column: "SigningSessionId");

        migrationBuilder.CreateIndex(
            name: "IX_SigningSessions_DocumentId",
            table: "SigningSessions",
            column: "DocumentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SignatureFields");

        migrationBuilder.DropTable(
            name: "SigningSessions");

        migrationBuilder.DropTable(
            name: "Documents");
    }
}
