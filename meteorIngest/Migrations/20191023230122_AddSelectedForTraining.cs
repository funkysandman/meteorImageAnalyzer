using Microsoft.EntityFrameworkCore.Migrations;

namespace meteorIngest.Migrations
{
    public partial class AddSelectedForTraining : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "selectedForTraining",
                table: "SkyImages",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selectedForTraining",
                table: "SkyImages");
        }
    }
}
