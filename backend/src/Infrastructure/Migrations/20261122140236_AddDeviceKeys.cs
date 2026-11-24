using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "device_key_created_at",
                table: "installations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "device_key_hash",
                table: "installations",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_key_prefix",
                table: "installations",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_installations_device_key_prefix",
                table: "installations",
                column: "device_key_prefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_installations_device_key_prefix",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "device_key_created_at",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "device_key_hash",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "device_key_prefix",
                table: "installations");
        }
    }
}
