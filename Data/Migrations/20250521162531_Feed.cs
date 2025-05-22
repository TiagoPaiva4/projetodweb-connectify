using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class Feed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "a3b119ca-fe9a-494f-8bd9-c5caaf1da86e");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7dbcba99-e2c9-4d11-9201-c5ea145390a6", "AQAAAAIAAYagAAAAEG5hJ1YL6QmZ7oVAlyNveu2uR/8ACXhBxT6yg7+8DHJxccaJ9ouU+mR76QbnmD84hA==", "bcc7416e-d54c-4d04-8264-98f427d04861" });
        }
    }
}
