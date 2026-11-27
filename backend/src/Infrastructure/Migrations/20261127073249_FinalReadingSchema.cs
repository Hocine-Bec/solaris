using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalReadingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The readings already stored are kept, in the new shape. The naive table let one slot be stored more
            // than once; the key about to be added won't, so the duplicates go first, the earliest row of each slot
            // staying, as BR-T1 says ("the first one wins").
            migrationBuilder.Sql(DropDuplicateSlotsSql);

            migrationBuilder.RenameColumn(
                name: "recorded_at",
                table: "energy_readings",
                newName: "slot_start");

            migrationBuilder.AddColumn<int>(
                name: "energy_wh",
                table: "energy_readings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Kilowatt-hours to whole watt-hours: a tenth of a watt-hour rounds to nothing, which no real reading is
            migrationBuilder.Sql("UPDATE energy_readings SET energy_wh = round(energy_kwh * 1000);");

            migrationBuilder.DropColumn(
                name: "energy_kwh",
                table: "energy_readings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_energy_readings",
                table: "energy_readings");

            migrationBuilder.DropColumn(
                name: "id",
                table: "energy_readings");

            // The key's own index starts with installation_id, so the foreign key's separate one is redundant
            migrationBuilder.DropIndex(
                name: "ix_energy_readings_installation_id",
                table: "energy_readings");

            migrationBuilder.AddPrimaryKey(
                name: "pk_energy_readings",
                table: "energy_readings",
                columns: new[] { "installation_id", "slot_start" });

            migrationBuilder.CreateTable(
                name: "production_dirty_days",
                columns: table => new
                {
                    installation_id = table.Column<int>(type: "integer", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_dirty_days", x => new { x.installation_id, x.day });
                    table.ForeignKey(
                        name: "fk_production_dirty_days_installations_installation_id",
                        column: x => x.installation_id,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <summary>
        /// Drops the slots the naive table let be stored more than once, keeping each slot's earliest row, as BR-T1
        /// says ("the first one wins"). Nothing else can hold two rows for one slot once the key is added.
        /// </summary>
        public const string DropDuplicateSlotsSql = """
            DELETE FROM energy_readings a
            USING energy_readings b
            WHERE a.installation_id = b.installation_id AND a.recorded_at = b.recorded_at AND a.id > b.id;
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_dirty_days");

            migrationBuilder.DropPrimaryKey(
                name: "pk_energy_readings",
                table: "energy_readings");

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "energy_readings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<decimal>(
                name: "energy_kwh",
                table: "energy_readings",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE energy_readings SET energy_kwh = energy_wh / 1000.0;");

            migrationBuilder.DropColumn(
                name: "energy_wh",
                table: "energy_readings");

            migrationBuilder.RenameColumn(
                name: "slot_start",
                table: "energy_readings",
                newName: "recorded_at");

            migrationBuilder.AddPrimaryKey(
                name: "pk_energy_readings",
                table: "energy_readings",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_energy_readings_installation_id",
                table: "energy_readings",
                column: "installation_id");
        }
    }
}
