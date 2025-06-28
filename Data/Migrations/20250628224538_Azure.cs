using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class Azure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigitalLibrary");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "8ca66972-fb3a-41ed-8678-2a83a74af871");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5dec5a97-f227-4d2a-a3f6-beaafcd5c393", "AQAAAAIAAYagAAAAEGm2YZHfI9a+XPHSoud7DRTbtJRKjV1Tcy1PIQy3lmx4mVbqD8h4Q/HiimcmMhDTzg==", "de2d2cd8-516f-4815-acc5-c4be85a0279c" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigitalLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResourceLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalLibrary_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
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
                values: new object[] { "7eb1f246-d908-4847-a71e-44e62cc4d33c", "AQAAAAIAAYagAAAAEBxNh8GOyc/C7SGP9bQGwog4p/o4nSfLT4SXrbnINfEtBHA6igIPTZ63WK8MY2uQ/Q==", "7d6fd552-c7c3-46ea-b518-02512d0e0a2f" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalLibrary_ProfileId",
                table: "DigitalLibrary",
                column: "ProfileId");
        }
    }
}
