using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLikeFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicCommentLikes",
                columns: table => new
                {
                    TopicCommentId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicCommentLikes", x => new { x.TopicCommentId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_TopicCommentLikes_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicCommentLikes_TopicComments_TopicCommentId",
                        column: x => x.TopicCommentId,
                        principalTable: "TopicComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicPostLikes",
                columns: table => new
                {
                    TopicPostId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicPostLikes", x => new { x.TopicPostId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_TopicPostLikes_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicPostLikes_TopicPosts_TopicPostId",
                        column: x => x.TopicPostId,
                        principalTable: "TopicPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "ce1bff26-1727-4adb-a28a-981c8af07023");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a86ab022-22f3-4e5a-953e-5739a741d7d0", "AQAAAAIAAYagAAAAEBxNh8GOyc/C7SGP9bQGwog4p/o4nSfLT4SXrbnINfEtBHA6igIPTZ63WK8MY2uQ/Q==", "7d6fd552-c7c3-46ea-b518-02512d0e0a2f" });

            migrationBuilder.CreateIndex(
                name: "IX_TopicCommentLikes_ProfileId",
                table: "TopicCommentLikes",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicPostLikes_ProfileId",
                table: "TopicPostLikes",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicCommentLikes");

            migrationBuilder.DropTable(
                name: "TopicPostLikes");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "f112ab3e-d100-431e-94af-0dddd5a5fb9e");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "67f1cd06-8c9a-4d82-8dc6-d9005d70c0e7", "AQAAAAIAAYagAAAAEEJop5nHIzfeMJRXbhYN+lqIXu5zVSOOPMGNvBgUzulBsejQdA1YFmcfk8AEP1hCnw==", "6b608457-f2ac-4ddc-9694-96be7f8ace64" });
        }
    }
}
