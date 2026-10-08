using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SemesterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.CheckConstraint("CK_CalendarEvents_AtMostOneParent", "(\"CourseId\" IS NULL OR \"SemesterId\" IS NULL)");
                    table.CheckConstraint("CK_CalendarEvents_Duration", "(\"DurationMinutes\" IS NULL OR (\"DurationMinutes\" >= 5 AND \"DurationMinutes\" <= 720))");
                    table.CheckConstraint("CK_CalendarEvents_DurationNeedsStart", "(\"DurationMinutes\" IS NULL OR \"StartTime\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_CourseId",
                table: "CalendarEvents",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_Date",
                table: "CalendarEvents",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_SemesterId",
                table: "CalendarEvents",
                column: "SemesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEvents");
        }
    }
}
