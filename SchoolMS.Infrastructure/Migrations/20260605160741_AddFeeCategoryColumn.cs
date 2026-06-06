using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeCategoryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FeeStructures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FeeCategory",
                table: "FeeStructures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOptional",
                table: "FeeStructures",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "FeeStructures");

            migrationBuilder.DropColumn(
                name: "FeeCategory",
                table: "FeeStructures");

            migrationBuilder.DropColumn(
                name: "IsOptional",
                table: "FeeStructures");
        }
    }
}
