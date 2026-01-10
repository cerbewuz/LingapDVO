using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesAndRemoveUserAccountNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename RegisterAcc table to UserAccount if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RegisterAcc')
                BEGIN
                    EXEC sp_rename 'RegisterAcc', 'UserAccount'
                END
            ");

            // Rename Verifyaccount table to VerifiedAccount if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Verifyaccount')
                BEGIN
                    EXEC sp_rename 'Verifyaccount', 'VerifiedAccount'
                END
            ");

            // Drop Useraccount table if it exists (legacy table)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Useraccount')
                BEGIN
                    DROP TABLE [Useraccount]
                END
            ");

            // Drop FirstName column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                          WHERE TABLE_NAME = 'UserAccount' AND COLUMN_NAME = 'FirstName')
                BEGIN
                    ALTER TABLE [UserAccount] DROP COLUMN [FirstName]
                END
            ");

            // Drop LastName column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                          WHERE TABLE_NAME = 'UserAccount' AND COLUMN_NAME = 'LastName')
                BEGIN
                    ALTER TABLE [UserAccount] DROP COLUMN [LastName]
                END
            ");

            // Drop MiddleName column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                          WHERE TABLE_NAME = 'UserAccount' AND COLUMN_NAME = 'MiddleName')
                BEGIN
                    ALTER TABLE [UserAccount] DROP COLUMN [MiddleName]
                END
            ");

            // Drop Suffix column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                          WHERE TABLE_NAME = 'UserAccount' AND COLUMN_NAME = 'Suffix')
                BEGIN
                    ALTER TABLE [UserAccount] DROP COLUMN [Suffix]
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back columns to UserAccount (or RegisterAcc if already renamed)
            migrationBuilder.Sql(@"
                DECLARE @TableName NVARCHAR(100)
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserAccount')
                    SET @TableName = 'UserAccount'
                ELSE IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RegisterAcc')
                    SET @TableName = 'RegisterAcc'

                IF @TableName IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                                  WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'FirstName')
                    BEGIN
                        EXEC('ALTER TABLE [' + @TableName + '] ADD [FirstName] nvarchar(100) NOT NULL DEFAULT ''''')
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                                  WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'LastName')
                    BEGIN
                        EXEC('ALTER TABLE [' + @TableName + '] ADD [LastName] nvarchar(100) NOT NULL DEFAULT ''''')
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                                  WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'MiddleName')
                    BEGIN
                        EXEC('ALTER TABLE [' + @TableName + '] ADD [MiddleName] nvarchar(100) NOT NULL DEFAULT ''''')
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                                  WHERE TABLE_NAME = @TableName AND COLUMN_NAME = 'Suffix')
                    BEGIN
                        EXEC('ALTER TABLE [' + @TableName + '] ADD [Suffix] nvarchar(50) NULL')
                    END
                END
            ");

            // Rename tables back to original names
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserAccount')
                BEGIN
                    EXEC sp_rename 'UserAccount', 'RegisterAcc'
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VerifiedAccount')
                BEGIN
                    EXEC sp_rename 'VerifiedAccount', 'Verifyaccount'
                END
            ");
        }
    }
}
