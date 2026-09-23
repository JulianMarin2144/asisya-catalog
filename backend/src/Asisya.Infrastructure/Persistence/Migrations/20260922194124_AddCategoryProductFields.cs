using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asisya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryProductFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "categories",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "categories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "categories");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrl",
                table: "categories",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
