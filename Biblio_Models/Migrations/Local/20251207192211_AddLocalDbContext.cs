using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblio_Models.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Naam = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorien", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Voornaam = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AchterNaam = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telefoon = table.Column<string>(type: "TEXT", nullable: true),
                    Adres = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leden", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Talen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Naam = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Talen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boeken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Auteur = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Isbn = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    CategorieID = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boeken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boeken_Categorien_CategorieID",
                        column: x => x.CategorieID,
                        principalTable: "Categorien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leningens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoekId = table.Column<int>(type: "INTEGER", nullable: false),
                    LidId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leningens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leningens_Boeken_BoekId",
                        column: x => x.BoekId,
                        principalTable: "Boeken",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leningens_Leden_LidId",
                        column: x => x.LidId,
                        principalTable: "Leden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boeken_CategorieID",
                table: "Boeken",
                column: "CategorieID");

            migrationBuilder.CreateIndex(
                name: "IX_Leningens_BoekId",
                table: "Leningens",
                column: "BoekId");

            migrationBuilder.CreateIndex(
                name: "IX_Leningens_LidId",
                table: "Leningens",
                column: "LidId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Leningens");

            migrationBuilder.DropTable(
                name: "Talen");

            migrationBuilder.DropTable(
                name: "Boeken");

            migrationBuilder.DropTable(
                name: "Leden");

            migrationBuilder.DropTable(
                name: "Categorien");
        }
    }
}
