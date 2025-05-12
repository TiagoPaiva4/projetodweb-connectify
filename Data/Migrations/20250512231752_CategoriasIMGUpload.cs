using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class CategoriasIMGUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryImageUrl",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryImageUrl",
                table: "Categories");
        }
    }
}
