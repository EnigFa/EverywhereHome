using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class HostApprovalAndBlocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('AspNetUsers', 'IsAdmin') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [IsAdmin] bit NOT NULL CONSTRAINT [DF_AspNetUsers_IsAdmin] DEFAULT CAST(0 AS bit);
                IF COL_LENGTH('AspNetUsers', 'IsBlocked') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [IsBlocked] bit NOT NULL CONSTRAINT [DF_AspNetUsers_IsBlocked] DEFAULT CAST(0 AS bit);
                IF OBJECT_ID(N'[HostApplications]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [HostApplications] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        [FullName] nvarchar(max) NOT NULL,
                        [DocumentUrl] nvarchar(max) NOT NULL,
                        [Status] int NOT NULL,
                        [AdminNote] nvarchar(max) NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_HostApplications] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_HostApplications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
                    );
                    CREATE INDEX [IX_HostApplications_UserId] ON [HostApplications] ([UserId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostApplications");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "AspNetUsers");
        }
    }
}
