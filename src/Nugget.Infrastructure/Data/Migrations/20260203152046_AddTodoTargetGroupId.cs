using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nugget.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoTargetGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "target_group_id",
                table: "todos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_todos_target_group_id",
                table: "todos",
                column: "target_group_id");

            migrationBuilder.AddForeignKey(
                name: "FK_todos_groups_target_group_id",
                table: "todos",
                column: "target_group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_todos_groups_target_group_id",
                table: "todos");

            migrationBuilder.DropIndex(
                name: "IX_todos_target_group_id",
                table: "todos");

            migrationBuilder.DropColumn(
                name: "target_group_id",
                table: "todos");
        }
    }
}
