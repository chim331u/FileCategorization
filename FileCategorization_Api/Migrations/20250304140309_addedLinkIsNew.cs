using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FC_RESTAPI.Migrations
{
    /// <inheritdoc />
    public partial class addedLinkIsNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "DDLinkEd2",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "DDLinkEd2");
        }
    }
}
