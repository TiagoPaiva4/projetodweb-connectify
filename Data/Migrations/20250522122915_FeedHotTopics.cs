using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedHotTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "4a068588-d502-4361-9986-c499514bc5b1");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "66ca4fdd-5bbf-4abb-a819-a1409bc17832", "AQAAAAIAAYagAAAAEGUWKwUP+/TPZEa23cS50fsXPnXEOP0k3LHx3gzoKvN4m0LR1xqc5Zy3Eg2QUDmBnA==", "825d75a6-d414-4ec0-a1c2-5e60b32d5ee2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "2cbb0cbc-c975-4437-83d6-ee5b7830348b");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "45de9384-f65e-47f1-a5b7-565d8c581211", "AQAAAAIAAYagAAAAEK9tVMknQP/NohhNLXWMhWPvSrwb2sO6VoA61zS3ihW7cyTf3fgfw0cpERt+XzOuog==", "d8f8d685-3c08-4b9d-82a9-2ebe0ab4593e" });
        }
    }
}
