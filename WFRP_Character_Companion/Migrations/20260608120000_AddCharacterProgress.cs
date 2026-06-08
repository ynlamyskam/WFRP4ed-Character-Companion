using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WFRP_Character_Companion.Migrations
{
    public partial class AddCharacterProgress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorruptionPoints",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceEarned",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceSpent",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CorruptionPoints", table: "Characters");
            migrationBuilder.DropColumn(name: "ExperienceEarned", table: "Characters");
            migrationBuilder.DropColumn(name: "ExperienceSpent", table: "Characters");
        }
    }
}
