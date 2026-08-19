using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvailityClaimResponseProcessor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RawContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StatusMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RejectionReasonMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PatientResponsibilityAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EdiFileRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    InterchangeControlNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    HasErrors = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdiFileRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaimErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimResponseId = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimErrors_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimResponseId = table.Column<int>(type: "int", nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatusCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SubmittedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PatientResponsibilityAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CheckNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimPayments_ClaimResponses_ClaimResponseId",
                        column: x => x.ClaimResponseId,
                        principalTable: "ClaimResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimErrors_ClaimResponseId",
                table: "ClaimErrors",
                column: "ClaimResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayments_ClaimResponseId",
                table: "ClaimPayments",
                column: "ClaimResponseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimResponses_ClaimId",
                table: "ClaimResponses",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_EdiFileRecords_FileName",
                table: "EdiFileRecords",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimErrors");

            migrationBuilder.DropTable(
                name: "ClaimPayments");

            migrationBuilder.DropTable(
                name: "EdiFileRecords");

            migrationBuilder.DropTable(
                name: "ClaimResponses");
        }
    }
}
