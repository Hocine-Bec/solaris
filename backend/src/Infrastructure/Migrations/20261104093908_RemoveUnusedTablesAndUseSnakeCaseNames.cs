using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedTablesAndUseSnakeCaseNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_addresses_contact_address_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_addresses_address_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "FK_leads_customers_customer_id",
                table: "leads");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "energy_production");

            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "installation_status_history");

            migrationBuilder.DropTable(
                name: "installation_technicians");

            migrationBuilder.DropTable(
                name: "support_tickets");

            migrationBuilder.DropTable(
                name: "weather_data");

            migrationBuilder.DropTable(
                name: "installations");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_leads",
                table: "leads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customers",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_addresses",
                table: "addresses");

            migrationBuilder.RenameIndex(
                name: "IX_leads_address_id",
                table: "leads",
                newName: "ix_leads_address_id");

            migrationBuilder.RenameIndex(
                name: "IX_customers_contact_address_id",
                table: "customers",
                newName: "ix_customers_contact_address_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_leads",
                table: "leads",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_customers",
                table: "customers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_addresses",
                table: "addresses",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_customers_addresses_contact_address_id",
                table: "customers",
                column: "contact_address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_leads_addresses_address_id",
                table: "leads",
                column: "address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_leads_customers_customer_id",
                table: "leads",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customers_addresses_contact_address_id",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_leads_addresses_address_id",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "fk_leads_customers_customer_id",
                table: "leads");

            migrationBuilder.DropPrimaryKey(
                name: "pk_leads",
                table: "leads");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customers",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_addresses",
                table: "addresses");

            migrationBuilder.RenameIndex(
                name: "ix_leads_address_id",
                table: "leads",
                newName: "IX_leads_address_id");

            migrationBuilder.RenameIndex(
                name: "ix_customers_contact_address_id",
                table: "customers",
                newName: "IX_customers_contact_address_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_leads",
                table: "leads",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customers",
                table: "customers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_addresses",
                table: "addresses",
                column: "id");

            migrationBuilder.CreateTable(
                name: "installations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    installation_address_id = table.Column<int>(type: "integer", nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inverter_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    panel_count = table.Column<int>(type: "integer", nullable: false),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    system_size_kw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installations", x => x.id);
                    table.ForeignKey(
                        name: "FK_installations_addresses_installation_address_id",
                        column: x => x.installation_address_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_installations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    license_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    specialization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weather_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cloud_cover_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    temperature_celsius = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstallationId = table.Column<int>(type: "integer", nullable: true),
                    cost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    specifications = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    warranty_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_equipment_installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "installation_status_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: false),
                    InstallationId = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_installation_status_history_installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_installation_status_history_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installation_technicians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstallationId = table.Column<int>(type: "integer", nullable: false),
                    TechnicianId = table.Column<int>(type: "integer", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installation_technicians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_installation_technicians_installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_installation_technicians_users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    InstallationId = table.Column<int>(type: "integer", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_tickets_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_support_tickets_installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_support_tickets_users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "energy_production",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstallationId = table.Column<int>(type: "integer", nullable: false),
                    WeatherDataId = table.Column<int>(type: "integer", nullable: true),
                    actual_production_kwh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    expected_production_kwh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    health_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    production_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_energy_production", x => x.Id);
                    table.ForeignKey(
                        name: "FK_energy_production_installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_energy_production_weather_data_WeatherDataId",
                        column: x => x.WeatherDataId,
                        principalTable: "weather_data",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    InstallationId = table.Column<int>(type: "integer", nullable: true),
                    TicketId = table.Column<int>(type: "integer", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "installations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_support_tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "support_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documents_customer_id",
                table: "documents",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "ix_documents_installation_id",
                table: "documents",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "ix_documents_ticket_id",
                table: "documents",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "ix_documents_type",
                table: "documents",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_at",
                table: "documents",
                column: "uploaded_at");

            migrationBuilder.CreateIndex(
                name: "IX_documents_UploadedByUserId",
                table: "documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_energy_production_date",
                table: "energy_production",
                column: "production_date");

            migrationBuilder.CreateIndex(
                name: "ix_energy_production_installation_date",
                table: "energy_production",
                columns: new[] { "InstallationId", "production_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_energy_production_installation_id",
                table: "energy_production",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_energy_production_WeatherDataId",
                table: "energy_production",
                column: "WeatherDataId");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_installation_id",
                table: "equipment",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_serial_number",
                table: "equipment",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipment_status",
                table: "equipment",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_type",
                table: "equipment",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_installation_status_history_changed_at",
                table: "installation_status_history",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "IX_installation_status_history_ChangedByUserId",
                table: "installation_status_history",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_installation_status_history_installation_changed",
                table: "installation_status_history",
                columns: new[] { "InstallationId", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_status_history_installation_id",
                table: "installation_status_history",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "ix_installation_technicians_installation_id",
                table: "installation_technicians",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "ix_installation_technicians_installation_technician",
                table: "installation_technicians",
                columns: new[] { "InstallationId", "TechnicianId" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_technicians_technician_id",
                table: "installation_technicians",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "ix_installations_customer_id",
                table: "installations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_installations_installation_address_id",
                table: "installations",
                column: "installation_address_id");

            migrationBuilder.CreateIndex(
                name: "ix_installations_status",
                table: "installations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_installations_status_start_date",
                table: "installations",
                columns: new[] { "status", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_assigned_to_user_id",
                table: "support_tickets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_created_at",
                table: "support_tickets",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_customer_id",
                table: "support_tickets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_InstallationId",
                table: "support_tickets",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_priority",
                table: "support_tickets",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_support_tickets_status",
                table: "support_tickets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_weather_data_date_location",
                table: "weather_data",
                columns: new[] { "date", "location" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_customers_addresses_contact_address_id",
                table: "customers",
                column: "contact_address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_addresses_address_id",
                table: "leads",
                column: "address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_leads_customers_customer_id",
                table: "leads",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
