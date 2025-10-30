using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNameKrToFood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_kr",
                table: "foods",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name_kr",
                table: "foods");
        }
    }
}
