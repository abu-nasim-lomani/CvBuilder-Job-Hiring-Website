using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvBuilder.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStylePreferencesToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StylePreferencesJson",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StylePreferencesJson",
                table: "UserProfiles");
        }
    }
}
