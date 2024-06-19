using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunBot2024.Migrations
{
    /// <inheritdoc />
    public partial class ReCreateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegionName",
                schema: "RunBot2024",
                table: "RegionList",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                schema: "RunBot2024",
                table: "CompanyList",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                schema: "RunBot2024",
                table: "CompanyList",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CityName",
                schema: "RunBot2024",
                table: "CityList",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                schema: "RunBot2024",
                table: "CityList",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegionName",
                schema: "RunBot2024",
                table: "RegionList");

            migrationBuilder.DropColumn(
                name: "CityId",
                schema: "RunBot2024",
                table: "CompanyList");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                schema: "RunBot2024",
                table: "CompanyList");

            migrationBuilder.DropColumn(
                name: "CityName",
                schema: "RunBot2024",
                table: "CityList");

            migrationBuilder.DropColumn(
                name: "RegionId",
                schema: "RunBot2024",
                table: "CityList");
        }
    }
}
