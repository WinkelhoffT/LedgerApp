using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudySessionCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualDurationMinutes",
                table: "StudySessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "StudySessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StudySessions_ActualDuration",
                table: "StudySessions",
                sql: "(\"ActualDurationMinutes\" IS NULL OR (\"ActualDurationMinutes\" >= 5 AND \"ActualDurationMinutes\" <= 720))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StudySessions_Completion",
                table: "StudySessions",
                sql: "((\"CompletedAt\" IS NULL AND \"ActualDurationMinutes\" IS NULL) OR (\"CompletedAt\" IS NOT NULL AND \"ActualDurationMinutes\" IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StudySessions_ActualDuration",
                table: "StudySessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StudySessions_Completion",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "ActualDurationMinutes",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "StudySessions");
        }
    }
}
