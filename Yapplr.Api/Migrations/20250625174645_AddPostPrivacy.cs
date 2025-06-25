using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Postr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPostPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Privacy",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Privacy",
                table: "Posts");
        }
    }
}
