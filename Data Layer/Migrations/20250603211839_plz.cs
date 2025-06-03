using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VROOM.Data.Migrations
{
    /// <inheritdoc />
    public partial class plz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RiderID",
                table: "Shipments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "Lastupdated",
                table: "Riders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Issues",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Issues",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Issues",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ShipmentID",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ShipmentID",
                table: "Issues",
                column: "ShipmentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Shipments_ShipmentID",
                table: "Issues",
                column: "ShipmentID",
                principalTable: "Shipments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Shipments_ShipmentID",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ShipmentID",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Lastupdated",
                table: "Riders");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ShipmentID",
                table: "Issues");

            migrationBuilder.AlterColumn<string>(
                name: "RiderID",
                table: "Shipments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
