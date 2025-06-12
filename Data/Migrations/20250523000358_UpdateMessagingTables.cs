using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessagingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_User1Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_User2Id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_User1Id",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "User2Id",
                table: "Conversations",
                newName: "Participant2Id");

            migrationBuilder.RenameColumn(
                name: "User1Id",
                table: "Conversations",
                newName: "Participant1Id");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_User2Id",
                table: "Conversations",
                newName: "IX_Conversations_Participant2Id");

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

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations",
                columns: new[] { "Participant1Id", "Participant2Id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_Participant1Id",
                table: "Conversations",
                column: "Participant1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_Participant2Id",
                table: "Conversations",
                column: "Participant2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_Participant1Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_Participant2Id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Participant1Id_Participant2Id",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "Participant2Id",
                table: "Conversations",
                newName: "User2Id");

            migrationBuilder.RenameColumn(
                name: "Participant1Id",
                table: "Conversations",
                newName: "User1Id");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_Participant2Id",
                table: "Conversations",
                newName: "IX_Conversations_User2Id");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a",
                column: "ConcurrencyStamp",
                value: "a8c18002-9dd1-40ef-91e7-fdb360dfdf45");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b3767706-9d8a-4e36-94ed-69042df2cd89", "AQAAAAIAAYagAAAAEKGFPfX7jTt6wKvnWZc1B+IUSkkqT5/G/l+fhTrlVj74Ot5q9WAb7HPTWFmaATjT3A==", "e1df6add-324b-4fc1-9402-78cc5541f24a" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_User1Id",
                table: "Conversations",
                column: "User1Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_User1Id",
                table: "Conversations",
                column: "User1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_User2Id",
                table: "Conversations",
                column: "User2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
