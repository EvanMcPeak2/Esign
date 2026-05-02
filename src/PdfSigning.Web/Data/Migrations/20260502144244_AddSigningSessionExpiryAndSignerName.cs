using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfSigning.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSigningSessionExpiryAndSignerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                table: "SigningSessions",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "SignedByName",
                table: "SigningSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "SigningSessions");

            migrationBuilder.DropColumn(
                name: "SignedByName",
                table: "SigningSessions");
        }
    }
}
