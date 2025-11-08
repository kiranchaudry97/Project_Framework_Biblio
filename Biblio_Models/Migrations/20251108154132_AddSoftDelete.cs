using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblio_Models.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gebruik conditionele T‑SQL zodat de migratie veilig is als kolommen al bestaan
            migrationBuilder.Sql(@"
IF COL_LENGTH('Boeken','IsDeleted') IS NULL
BEGIN
    ALTER TABLE [Boeken] ADD [IsDeleted] bit NOT NULL CONSTRAINT DF_Boeken_IsDeleted DEFAULT(0);
    ALTER TABLE [Boeken] ADD [DeletedAt] datetime2 NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Leden','IsDeleted') IS NULL
BEGIN
    ALTER TABLE [Leden] ADD [IsDeleted] bit NOT NULL CONSTRAINT DF_Leden_IsDeleted DEFAULT(0);
    ALTER TABLE [Leden] ADD [DeletedAt] datetime2 NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Categorien','IsDeleted') IS NULL
BEGIN
    ALTER TABLE [Categorien] ADD [IsDeleted] bit NOT NULL CONSTRAINT DF_Categorien_IsDeleted DEFAULT(0);
    ALTER TABLE [Categorien] ADD [DeletedAt] datetime2 NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Uitleningen','IsDeleted') IS NULL
BEGIN
    ALTER TABLE [Uitleningen] ADD [IsDeleted] bit NOT NULL CONSTRAINT DF_Uitleningen_IsDeleted DEFAULT(0);
    ALTER TABLE [Uitleningen] ADD [DeletedAt] datetime2 NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Verwijder kolommen als ze bestaan
            migrationBuilder.Sql(@"
IF COL_LENGTH('Boeken','IsDeleted') IS NOT NULL
BEGIN
    ALTER TABLE [Boeken] DROP COLUMN [DeletedAt];
    ALTER TABLE [Boeken] DROP COLUMN [IsDeleted];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Leden','IsDeleted') IS NOT NULL
BEGIN
    ALTER TABLE [Leden] DROP COLUMN [DeletedAt];
    ALTER TABLE [Leden] DROP COLUMN [IsDeleted];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Categorien','IsDeleted') IS NOT NULL
BEGIN
    ALTER TABLE [Categorien] DROP COLUMN [DeletedAt];
    ALTER TABLE [Categorien] DROP COLUMN [IsDeleted];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Uitleningen','IsDeleted') IS NOT NULL
BEGIN
    ALTER TABLE [Uitleningen] DROP COLUMN [DeletedAt];
    ALTER TABLE [Uitleningen] DROP COLUMN [IsDeleted];
END
");
        }
    }
}
