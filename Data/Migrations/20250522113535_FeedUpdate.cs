using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "d6a7c6c7-b3fd-4334-a67f-d1e072e4089a");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8a6917c2-75fb-4363-a55b-6047c7dacda7", "AQAAAAIAAYagAAAAEIz5BfA8KqAJripMAzmoPmZDQZM4OEpPfOuicp5hmoulGN9oyIck2ckAAsuqWEq9kg==", "d7285f36-a229-41b7-a3f0-2420d3293428" });
        }
    }
}
