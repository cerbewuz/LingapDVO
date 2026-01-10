using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class CreateMissingUserAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserAccount table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserAccount')
                BEGIN
                    CREATE TABLE [UserAccount] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [Email] nvarchar(100) NOT NULL,
                        [Password] nvarchar(250) NOT NULL,
                        [Username] nvarchar(100) NOT NULL,
                        [Status] nvarchar(max) NULL,
                        [Profilepicture] nvarchar(500) NULL,
                        [PreferEmailNotification] bit NOT NULL DEFAULT 1,
                        [PreferSmsNotification] bit NOT NULL DEFAULT 1,
                        [PreferInAppNotification] bit NOT NULL DEFAULT 1,
                        CONSTRAINT [PK_UserAccount] PRIMARY KEY ([Id])
                    );

                    CREATE UNIQUE INDEX [IX_UserAccount_Email] ON [UserAccount] ([Email]);
                    CREATE UNIQUE INDEX [IX_UserAccount_Username] ON [UserAccount] ([Username]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop UserAccount table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserAccount')
                BEGIN
                    DROP TABLE [UserAccount]
                END
            ");
        }
    }
}
