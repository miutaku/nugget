using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nugget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    display_name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    external_id = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    slack_user_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    role = table.Column<string>(type: "longtext", nullable: false),
                    saml_name_id = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    external_id = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    department = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    division = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    job_title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    employee_number = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    cost_center = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    organization = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    days_before_due = table.Column<string>(type: "longtext", nullable: false),
                    slack_notification_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    notification_hour = table.Column<int>(type: "int", nullable: false, defaultValue: 9),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "todos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    title = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "longtext", nullable: true),
                    due_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: false),
                    target_type = table.Column<string>(type: "longtext", nullable: false),
                    target_group_name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    target_group_id = table.Column<Guid>(type: "char(36)", nullable: true),
                    target_attribute_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    target_attribute_value = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    notify_immediately = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    reminder_days = table.Column<string>(type: "longtext", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todos", x => x.id);
                    table.ForeignKey(
                        name: "FK_todos_groups_target_group_id",
                        column: x => x.target_group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_todos_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    group_id = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_groups", x => new { x.user_id, x.group_id });
                    table.ForeignKey(
                        name: "FK_user_groups_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_groups_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "todo_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false),
                    todo_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false),
                    is_completed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_notified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_todo_assignments_todos_todo_id",
                        column: x => x.todo_id,
                        principalTable: "todos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_todo_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_groups_external_id",
                table: "groups",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_settings_user_id",
                table: "notification_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_todo_assignments_todo_id_user_id",
                table: "todo_assignments",
                columns: new[] { "todo_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_todo_assignments_user_id",
                table: "todo_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_todos_created_by",
                table: "todos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_todos_due_date",
                table: "todos",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "IX_todos_target_group_id",
                table: "todos",
                column: "target_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_group_id",
                table: "user_groups",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_department",
                table: "users",
                column: "department");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_external_id",
                table: "users",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_saml_name_id",
                table: "users",
                column: "saml_name_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_settings");

            migrationBuilder.DropTable(
                name: "todo_assignments");

            migrationBuilder.DropTable(
                name: "user_groups");

            migrationBuilder.DropTable(
                name: "todos");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
