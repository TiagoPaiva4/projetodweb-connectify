using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdjustedUserRegistrationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
            name: "IdentityUserId",
            table: "Users",
            type: "nvarchar(450)",
            nullable: true); // Agora a coluna pode ser nula (temporariamente)
                      // Remova defaultValue:""

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityUserId",
                table: "Users",
                column: "IdentityUserId");

            migrationBuilder.AddForeignKey(
    name: "FK_Users_AspNetUsers_IdentityUserId",
    table: "Users",
    column: "IdentityUserId",
    principalTable: "AspNetUsers",
    principalColumn: "Id",
    onDelete: ReferentialAction.Cascade); // <-- MUDAR PARA Restrict
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_AspNetUsers_IdentityUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Users");
        }
    }
}
