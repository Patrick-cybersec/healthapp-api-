using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthAppApi.Migrations
{
    public partial class SyncExercisesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table already created manually
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("exercises");
        }
    }
}