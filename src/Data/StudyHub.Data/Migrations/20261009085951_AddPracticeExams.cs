using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeExams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeExams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceKind = table.Column<int>(type: "INTEGER", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeckId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FocusHint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExams", x => x.Id);
                    table.CheckConstraint("CK_PracticeExams_Source", "((\"SourceKind\" = 0 AND \"CourseId\" IS NOT NULL AND \"DeckId\" IS NULL) OR (\"SourceKind\" = 1 AND \"DeckId\" IS NOT NULL AND \"CourseId\" IS NULL))");
                    table.ForeignKey(
                        name: "FK_PracticeExams_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeExams_FlashcardDecks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "FlashcardDecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticeExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedPoints = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeExamAttempts_PracticeExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "PracticeExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeExamTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Solution = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    SourceNoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceFlashcardId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsExcluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExamTasks", x => x.Id);
                    table.CheckConstraint("CK_PracticeExamTasks_AtMostOneSource", "(\"SourceNoteId\" IS NULL OR \"SourceFlashcardId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_PracticeExamTasks_Flashcards_SourceFlashcardId",
                        column: x => x.SourceFlashcardId,
                        principalTable: "Flashcards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PracticeExamTasks_Notes_SourceNoteId",
                        column: x => x.SourceNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeExamTasks_PracticeExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "PracticeExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeExamCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExamCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeExamCriteria_PracticeExamTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PracticeExamTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeExamOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExamOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeExamOptions_PracticeExamTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PracticeExamTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeExamAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AnswerText = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    AwardedPoints = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExamAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeExamAnswers_PracticeExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "PracticeExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeExamAnswers_PracticeExamOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "PracticeExamOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeExamAnswers_PracticeExamTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PracticeExamTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticeExamAnswerCriteria",
                columns: table => new
                {
                    AnswerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CriterionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeExamAnswerCriteria", x => new { x.AnswerId, x.CriterionId });
                    table.ForeignKey(
                        name: "FK_PracticeExamAnswerCriteria_PracticeExamAnswers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "PracticeExamAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeExamAnswerCriteria_PracticeExamCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "PracticeExamCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamAnswerCriteria_CriterionId",
                table: "PracticeExamAnswerCriteria",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamAnswers_AttemptId_TaskId",
                table: "PracticeExamAnswers",
                columns: new[] { "AttemptId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamAnswers_SelectedOptionId",
                table: "PracticeExamAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamAnswers_TaskId",
                table: "PracticeExamAnswers",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamAttempts_ExamId_StartedAt",
                table: "PracticeExamAttempts",
                columns: new[] { "ExamId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamCriteria_TaskId_Position",
                table: "PracticeExamCriteria",
                columns: new[] { "TaskId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamOptions_TaskId_Position",
                table: "PracticeExamOptions",
                columns: new[] { "TaskId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExams_CourseId",
                table: "PracticeExams",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExams_DeckId",
                table: "PracticeExams",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamTasks_ExamId_Position",
                table: "PracticeExamTasks",
                columns: new[] { "ExamId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamTasks_SourceFlashcardId",
                table: "PracticeExamTasks",
                column: "SourceFlashcardId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeExamTasks_SourceNoteId",
                table: "PracticeExamTasks",
                column: "SourceNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeExamAnswerCriteria");

            migrationBuilder.DropTable(
                name: "PracticeExamAnswers");

            migrationBuilder.DropTable(
                name: "PracticeExamCriteria");

            migrationBuilder.DropTable(
                name: "PracticeExamAttempts");

            migrationBuilder.DropTable(
                name: "PracticeExamOptions");

            migrationBuilder.DropTable(
                name: "PracticeExamTasks");

            migrationBuilder.DropTable(
                name: "PracticeExams");
        }
    }
}
