using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FC_RESTAPI.Migrations
{
    /// <inheritdoc />
    public partial class addedIsUsedinLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "DDLinkEd2",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "DDLinkEd2");
        }
    }
}
