using Microsoft.EntityFrameworkCore.Migrations;

namespace AquaShine.ApiHub.Data.Migrations
{
    public partial class AddRejectionColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Rejected",
                table: "Submissions",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rejected",
                table: "Submissions");
        }
    }
}
