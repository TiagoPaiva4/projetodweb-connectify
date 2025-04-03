using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdateModel1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Telemovel",
                table: "Users",
                newName: "Phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Users",
                newName: "Telemovel");
        }
    }
}
