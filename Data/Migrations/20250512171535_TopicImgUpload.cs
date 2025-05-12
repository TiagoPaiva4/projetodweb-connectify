using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projetodweb_connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class TopicImgUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TopicImageUrl",
                table: "Topics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TopicImageUrl",
                table: "Topics");
        }
    }
}
