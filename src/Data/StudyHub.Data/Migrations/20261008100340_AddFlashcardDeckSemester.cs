using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashcardDeckSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "FlashcardDecks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardDecks_SemesterId",
                table: "FlashcardDecks",
                column: "SemesterId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FlashcardDecks_AtMostOneParent",
                table: "FlashcardDecks",
                sql: "(\"CourseId\" IS NULL OR \"SemesterId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_FlashcardDecks_Semesters_SemesterId",
                table: "FlashcardDecks",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlashcardDecks_Semesters_SemesterId",
                table: "FlashcardDecks");

            migrationBuilder.DropIndex(
                name: "IX_FlashcardDecks_SemesterId",
                table: "FlashcardDecks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FlashcardDecks_AtMostOneParent",
                table: "FlashcardDecks");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "FlashcardDecks");
        }
    }
}
