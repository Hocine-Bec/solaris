using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "last_activity_date",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "converted_at",
                table: "leads",
                newName: "closed_at");

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            // Existing customers get their email as it is compared (Emails.Normalize: trimmed, upper-cased) before the
            // unique index is made.
            migrationBuilder.Sql("UPDATE customers SET normalized_email = upper(btrim(email));");

            migrationBuilder.CreateTable(
                name: "installations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    address_id = table.Column<int>(type: "integer", nullable: false),
                    lead_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installations", x => x.id);
                    table.ForeignKey(
                        name: "fk_installations_addresses_address_id",
                        column: x => x.address_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installations_leads_lead_id",
                        column: x => x.lead_id,
                        principalTable: "leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_normalized_email",
                table: "customers",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installations_address_id",
                table: "installations",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_installations_customer_id",
                table: "installations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_installations_lead_id",
                table: "installations",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_installations_status",
                table: "installations",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installations");

            migrationBuilder.DropIndex(
                name: "ix_customers_normalized_email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "closed_at",
                table: "leads",
                newName: "converted_at");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_activity_date",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                table: "customers",
                column: "email",
                unique: true);
        }
    }
}
