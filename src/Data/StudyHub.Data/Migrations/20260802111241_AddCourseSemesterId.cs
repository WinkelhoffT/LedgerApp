using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSemesterId : Migration
    {
        private static readonly Guid LegacySemesterId = new("00000000-0000-0000-0000-000000000001");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            // Courses pre-date the Semester relationship, so backfill any existing rows onto a
            // placeholder semester before the column is locked down to NOT NULL below.
            migrationBuilder.Sql($"""
                INSERT INTO "Semesters" ("Id", "Name", "StartDate", "EndDate", "IsArchived", "CreatedAt", "UpdatedAt")
                SELECT '{LegacySemesterId}', 'Legacy', '0001-01-01', '9999-12-31', 0, datetime('now'), datetime('now')
                WHERE EXISTS (SELECT 1 FROM "Courses" WHERE "SemesterId" IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM "Semesters" WHERE "Id" = '{LegacySemesterId}');
                """);

            migrationBuilder.Sql($"""
                UPDATE "Courses"
                SET "SemesterId" = '{LegacySemesterId}'
                WHERE "SemesterId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SemesterId",
                table: "Courses",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SemesterId",
                table: "Courses",
                column: "SemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                table: "Courses",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_SemesterId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Courses");

            migrationBuilder.Sql($"""
                DELETE FROM "Semesters" WHERE "Id" = '{LegacySemesterId}';
                """);
        }
    }
}
