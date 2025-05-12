using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class SavedTopic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopicPosts_Profiles_ProfileId",
                table: "TopicPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Profiles_CreatedBy",
                table: "Topics");

            migrationBuilder.CreateTable(
                name: "SavedTopics",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTopics", x => new { x.ProfileId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_SavedTopics_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedTopics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedTopics_TopicId",
                table: "SavedTopics",
                column: "TopicId");

            migrationBuilder.AddForeignKey(
                name: "FK_TopicPosts_Profiles_ProfileId",
                table: "TopicPosts",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_Profiles_CreatedBy",
                table: "Topics",
                column: "CreatedBy",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TopicPosts_Profiles_ProfileId",
                table: "TopicPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_Topics_Profiles_CreatedBy",
                table: "Topics");

            migrationBuilder.DropTable(
                name: "SavedTopics");

            migrationBuilder.AddForeignKey(
                name: "FK_TopicPosts_Profiles_ProfileId",
                table: "TopicPosts",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Topics_Profiles_CreatedBy",
                table: "Topics",
                column: "CreatedBy",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
