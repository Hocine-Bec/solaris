using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "give_up_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Messages saved before this column: their 24 hours end where they always did
            migrationBuilder.Sql("UPDATE outbox_messages SET give_up_at = created_at + interval '24 hours';");

            migrationBuilder.AddColumn<DateTime>(
                name: "retried_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retried_by_user_id",
                table: "outbox_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_failed",
                table: "outbox_messages",
                column: "last_attempt_at",
                filter: "status = 'Failed'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_retried_by_user_id",
                table: "outbox_messages",
                column: "retried_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_sent",
                table: "outbox_messages",
                column: "sent_at",
                filter: "status = 'Sent'");

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_messages_users_retried_by_user_id",
                table: "outbox_messages",
                column: "retried_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outbox_messages_users_retried_by_user_id",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_failed",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_retried_by_user_id",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_sent",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "give_up_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "retried_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "retried_by_user_id",
                table: "outbox_messages");
        }
    }
}
