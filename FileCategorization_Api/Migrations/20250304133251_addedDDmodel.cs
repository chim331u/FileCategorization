using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FC_RESTAPI.Migrations
{
    /// <inheritdoc />
    public partial class addedDDmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.CreateTable(
            //     name: "Configuration",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "INTEGER", nullable: false)
            //             .Annotation("Sqlite:Autoincrement", true),
            //         Key = table.Column<string>(type: "TEXT", nullable: false),
            //         Value = table.Column<string>(type: "TEXT", nullable: false),
            //         IsDev = table.Column<bool>(type: "INTEGER", nullable: false),
            //         CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
            //         LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
            //         IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
            //         Note = table.Column<string>(type: "TEXT", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Configuration", x => x.Id);
            //     });

            migrationBuilder.CreateTable(
                name: "DDSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Dd_User = table.Column<string>(type: "TEXT", nullable: false),
                    Dd_Password = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DDSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DDThreads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    MainTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DDThreads", x => x.Id);
                });

            // migrationBuilder.CreateTable(
            //     name: "FilesDetail",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "INTEGER", nullable: false)
            //             .Annotation("Sqlite:Autoincrement", true),
            //         Name = table.Column<string>(type: "TEXT", nullable: true),
            //         Path = table.Column<string>(type: "TEXT", nullable: true),
            //         FileSize = table.Column<double>(type: "REAL", nullable: false),
            //         LastUpdateFile = table.Column<DateTime>(type: "TEXT", nullable: false),
            //         FileCategory = table.Column<string>(type: "TEXT", nullable: true),
            //         IsToCategorize = table.Column<bool>(type: "INTEGER", nullable: false),
            //         IsNew = table.Column<bool>(type: "INTEGER", nullable: false),
            //         IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
            //         IsNotToMove = table.Column<bool>(type: "INTEGER", nullable: false),
            //         CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
            //         LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
            //         IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
            //         Note = table.Column<string>(type: "TEXT", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_FilesDetail", x => x.Id);
            //     });

            migrationBuilder.CreateTable(
                name: "DDLinkEd2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ed2kLink = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ThreadsId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DDLinkEd2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DDLinkEd2_DDThreads_ThreadsId",
                        column: x => x.ThreadsId,
                        principalTable: "DDThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DDLinkEd2_ThreadsId",
                table: "DDLinkEd2",
                column: "ThreadsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropTable(
            //     name: "Configuration");

            migrationBuilder.DropTable(
                name: "DDLinkEd2");

            migrationBuilder.DropTable(
                name: "DDSettings");

            // migrationBuilder.DropTable(
            //     name: "FilesDetail");

            migrationBuilder.DropTable(
                name: "DDThreads");
        }
    }
}
