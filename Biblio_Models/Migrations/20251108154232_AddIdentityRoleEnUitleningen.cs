using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblio_Models.Migrations
{
    /// <inheritdoc 
    /// zie commit bericht />
    public partial class AddIdentityRoleEnUitleningen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Zorg dat de rollen bestaan (idempotent)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'ADMIN')
BEGIN
    INSERT INTO [AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp]) VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'MEDEWERKER')
BEGIN
    INSERT INTO [AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp]) VALUES (NEWID(), 'Medewerker', 'MEDEWERKER', NEWID())
END
");

            // If Uitleningen table does not exist, create it (idempotent check)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Uitleningen')
BEGIN
    CREATE TABLE [Uitleningen]
    (
        [UitleningId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BoekId] INT NOT NULL,
        [LidId] INT NOT NULL,
        [StartDatum] DATETIME2 NOT NULL,
        [EindDatum] DATETIME2 NOT NULL,
        [IngeleverdOp] DATETIME2 NULL,
        [IsClosed] BIT NOT NULL DEFAULT(0),
        [IsDeleted] BIT NOT NULL DEFAULT(0),
        [DeletedAt] DATETIME2 NULL
    );
    -- Foreign keys (only add if referenced tables exist)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Boeken')
    BEGIN
        ALTER TABLE [Uitleningen] ADD CONSTRAINT FK_Uitleningen_Boeken FOREIGN KEY([BoekId]) REFERENCES [Boeken]([BoekId]) ON DELETE CASCADE;
    END
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Leden')
    BEGIN
        ALTER TABLE [Uitleningen] ADD CONSTRAINT FK_Uitleningen_Leden FOREIGN KEY([LidId]) REFERENCES [Leden]([LidId]) ON DELETE CASCADE;
    END
    CREATE INDEX IX_Uitleningen_LidId ON [Uitleningen]([LidId]);
    -- Unique active loan index
    CREATE UNIQUE INDEX IX_Uitleningen_BoekId_Actief ON [Uitleningen]([BoekId]) WHERE [IngeleverdOp] IS NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Verwijder rollen (indien aanwezig)
            migrationBuilder.Sql(@"
DELETE FROM [AspNetRoles] WHERE [NormalizedName] IN ('ADMIN','MEDEWERKER');
");

            // Drop Uitleningen table if it was created by this migration and exists
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Uitleningen')
BEGIN
    DROP TABLE [Uitleningen];
END
");
        }
    }
}
