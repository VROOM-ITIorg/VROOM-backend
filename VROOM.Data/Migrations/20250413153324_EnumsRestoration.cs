using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VROOM.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnumsRestoration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "State",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.InsertData(
                table: "Shipments",
                columns: new[] { "Id", "Beginning", "End", "IsDeleted", "MaxConsecutiveDeliveries", "ModifiedAt", "ModifiedBy", "RiderID", "RiderId", "Status" },
                values: new object[] { 2, new DateTime(2025, 4, 11, 9, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 4, 11, 13, 0, 0, 0, DateTimeKind.Unspecified), false, 3, new DateTime(2025, 4, 11, 9, 0, 0, 0, DateTimeKind.Unspecified), "TestUser2", 1, null, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Shipments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.AlterColumn<int>(
                name: "State",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
