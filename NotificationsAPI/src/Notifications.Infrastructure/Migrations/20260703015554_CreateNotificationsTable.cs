using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Infrastructure.Migrations;

/// <inheritdoc />
public partial class CreateNotificationsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                RecipientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                Subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                EventId = table.Column<Guid>(type: "uuid", nullable: true),
                RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notifications", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "idx_notification_created_at",
            table: "notifications",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "idx_notification_event_id",
            table: "notifications",
            column: "EventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_notification_status",
            table: "notifications",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "idx_notification_user_id",
            table: "notifications",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notifications");
    }
}
