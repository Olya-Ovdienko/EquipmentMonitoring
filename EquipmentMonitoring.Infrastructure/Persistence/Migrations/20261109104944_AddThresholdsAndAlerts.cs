using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThresholdsAndAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeasurementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Measurements_MeasurementId",
                        column: x => x.MeasurementId,
                        principalTable: "Measurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Thresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeasurementType = table.Column<int>(type: "INTEGER", nullable: false),
                    MinValue = table.Column<double>(type: "REAL", nullable: true),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Thresholds_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_EquipmentId_IsAcknowledged",
                table: "Alerts",
                columns: new[] { "EquipmentId", "IsAcknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_EquipmentId_Timestamp",
                table: "Alerts",
                columns: new[] { "EquipmentId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_MeasurementId",
                table: "Alerts",
                column: "MeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_Thresholds_EquipmentId_MeasurementType_Severity",
                table: "Thresholds",
                columns: new[] { "EquipmentId", "MeasurementType", "Severity" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Thresholds");
        }
    }
}
