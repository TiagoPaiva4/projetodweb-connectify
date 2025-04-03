using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Telemovel",
                table: "Users",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Telemovel",
                table: "Users");
        }
    }
}
