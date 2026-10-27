using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_On_PaymentBatch_AssignedTask_MobileDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeePaymentBatchId",
                table: "EmployeePayments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeAssignedTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskStatus = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssignedTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssignedTasks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeMobileDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeMobileDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeMobileDevices_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePaymentBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePaymentBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePaymentBatches_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePaymentBatches_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePaymentBatchDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeePaymentBatchId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePaymentBatchDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePaymentBatchDetails_EmployeePaymentBatches_EmployeePaymentBatchId",
                        column: x => x.EmployeePaymentBatchId,
                        principalTable: "EmployeePaymentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePaymentBatchDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_EmployeePaymentBatchId",
                table: "EmployeePayments",
                column: "EmployeePaymentBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_TenantId_EmployeePaymentBatchId",
                table: "EmployeePayments",
                columns: new[] { "TenantId", "EmployeePaymentBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignedTasks_EmployeeId",
                table: "EmployeeAssignedTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignedTasks_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeAssignedTasks",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMobileDevices_EmployeeId",
                table: "EmployeeMobileDevices",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMobileDevices_TenantId_DeviceId",
                table: "EmployeeMobileDevices",
                columns: new[] { "TenantId", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMobileDevices_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeMobileDevices",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentBatchDetails_EmployeeId",
                table: "EmployeePaymentBatchDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentBatchDetails_EmployeePaymentBatchId",
                table: "EmployeePaymentBatchDetails",
                column: "EmployeePaymentBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentBatchDetails_TenantId_EmployeePaymentBatchId",
                table: "EmployeePaymentBatchDetails",
                columns: new[] { "TenantId", "EmployeePaymentBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentBatches_CurrencyId",
                table: "EmployeePaymentBatches",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentBatches_TenantId_WorkflowStatus",
                table: "EmployeePaymentBatches",
                columns: new[] { "TenantId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePaymentBatches_WageTypeId",
                table: "EmployeePaymentBatches",
                column: "WageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePayments_EmployeePaymentBatches_EmployeePaymentBatchId",
                table: "EmployeePayments",
                column: "EmployeePaymentBatchId",
                principalTable: "EmployeePaymentBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePayments_EmployeePaymentBatches_EmployeePaymentBatchId",
                table: "EmployeePayments");

            migrationBuilder.DropTable(
                name: "EmployeeAssignedTasks");

            migrationBuilder.DropTable(
                name: "EmployeeMobileDevices");

            migrationBuilder.DropTable(
                name: "EmployeePaymentBatchDetails");

            migrationBuilder.DropTable(
                name: "EmployeePaymentBatches");

            migrationBuilder.DropIndex(
                name: "IX_EmployeePayments_EmployeePaymentBatchId",
                table: "EmployeePayments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeePayments_TenantId_EmployeePaymentBatchId",
                table: "EmployeePayments");

            migrationBuilder.DropColumn(
                name: "EmployeePaymentBatchId",
                table: "EmployeePayments");
        }
    }
}
