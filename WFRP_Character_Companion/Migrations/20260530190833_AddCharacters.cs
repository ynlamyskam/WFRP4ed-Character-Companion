using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WFRP_Character_Companion.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    WeaponSkillBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    BallisticSkillBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    StrengthBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    ToughnessBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    InitiativeBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    AgilityBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    DexterityBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    IntelligenceBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    WillpowerBasic = table.Column<int>(type: "INTEGER", nullable: false),
                    FellowshipBasic = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
