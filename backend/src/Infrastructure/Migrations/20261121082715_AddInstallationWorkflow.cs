using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "lead_id",
                table: "outbox_messages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "installation_id",
                table: "outbox_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "activated_at",
                table: "installations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inverter_type",
                table: "installations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "panel_count",
                table: "installations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "status_changed_at",
                table: "installations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "system_size_kw",
                table: "installations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "installation_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    installation_id = table.Column<int>(type: "integer", nullable: false),
                    technician_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installation_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_installation_assignments_installations_installation_id",
                        column: x => x.installation_id,
                        principalTable: "installations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_installation_assignments_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installation_assignments_users_completed_by_user_id",
                        column: x => x.completed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installation_assignments_users_technician_id",
                        column: x => x.technician_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installation_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    installation_id = table.Column<int>(type: "integer", nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installation_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_installation_status_history_installations_installation_id",
                        column: x => x.installation_id,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_installation_status_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Existing installations (all created by a conversion, all at Survey): their status changed when they were
            // made, and their history starts with that creation; who converted them wasn't recorded.
            migrationBuilder.Sql("UPDATE installations SET status_changed_at = created_at;");
            migrationBuilder.Sql("""
                INSERT INTO installation_status_history (installation_id, from_status, to_status, changed_at)
                SELECT id, NULL, status, created_at FROM installations;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_outbox_messages_installation_id_type",
                table: "outbox_messages",
                columns: new[] { "installation_id", "type" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_messages_one_subject",
                table: "outbox_messages",
                sql: "num_nonnulls(lead_id, installation_id) = 1");

            migrationBuilder.CreateIndex(
                name: "ix_installations_status_changed_at_id",
                table: "installations",
                columns: new[] { "status_changed_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_assignments_assigned_by_user_id",
                table: "installation_assignments",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_assignments_completed_by_user_id",
                table: "installation_assignments",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_assignments_technician_id",
                table: "installation_assignments",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ux_installation_assignments_installation_technician_role",
                table: "installation_assignments",
                columns: new[] { "installation_id", "technician_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installation_status_history_changed_by_user_id",
                table: "installation_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_installation_status_history_installation_id_changed_at",
                table: "installation_status_history",
                columns: new[] { "installation_id", "changed_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_messages_installations_installation_id",
                table: "outbox_messages",
                column: "installation_id",
                principalTable: "installations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outbox_messages_installations_installation_id",
                table: "outbox_messages");

            migrationBuilder.DropTable(
                name: "installation_assignments");

            migrationBuilder.DropTable(
                name: "installation_status_history");

            migrationBuilder.DropIndex(
                name: "ux_outbox_messages_installation_id_type",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_messages_one_subject",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_installations_status_changed_at_id",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "installation_id",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "activated_at",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "inverter_type",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "panel_count",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "status_changed_at",
                table: "installations");

            migrationBuilder.DropColumn(
                name: "system_size_kw",
                table: "installations");

            migrationBuilder.AlterColumn<int>(
                name: "lead_id",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
