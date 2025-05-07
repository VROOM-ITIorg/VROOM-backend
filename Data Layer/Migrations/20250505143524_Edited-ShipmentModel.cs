using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VROOM.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditedShipmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InTransiteBeginTime",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PrepareTime",
                table: "Orders",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InTransiteBeginTime",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PrepareTime",
                table: "Orders");
        }
    }
}
