using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WFRP_Character_Companion.Migrations
{
    /// <inheritdoc />
    public partial class AddTalentSpecialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "CharacterTalents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "CharacterTalents");
        }
    }
}
