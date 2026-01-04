using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblio_Models.Migrations
{
    /// <summary>
    /// Adds the missing IsDefault column to the Talen table when older databases were created before the property existed.
    /// </summary>
    public partial class AddMissingIsDefaultOnTalen : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Talen', 'IsDefault') IS NULL
BEGIN
    ALTER TABLE dbo.Talen ADD IsDefault bit NOT NULL CONSTRAINT DF_Talen_IsDefault DEFAULT(0);
    ALTER TABLE dbo.Talen DROP CONSTRAINT DF_Talen_IsDefault;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Talen', 'IsDefault') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Talen DROP COLUMN IsDefault;
END
");
        }
    }
}
