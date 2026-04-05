using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailApp.Migrations.AlarmDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmMaster",
                columns: table => new
                {
                    AlarmId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    CauseId = table.Column<int>(type: "int", nullable: true),
                    OriginationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmMaster", x => x.AlarmId);
                });

            migrationBuilder.CreateTable(
                name: "AlarmDetail",
                columns: table => new
                {
                    AlarmDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmId = table.Column<int>(type: "int", nullable: false),
                    AlarmState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventStamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    CommentId = table.Column<int>(type: "int", nullable: true),
                    OperatorID = table.Column<int>(type: "int", nullable: true),
                    AlarmTransition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlarmType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransitionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransitionTimeFracSec = table.Column<short>(type: "smallint", nullable: true),
                    TransitionTimeZoneOffset = table.Column<short>(type: "smallint", nullable: true),
                    TransitionDaylightAdjustment = table.Column<short>(type: "smallint", nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatorNode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmDetail", x => x.AlarmDetailId);
                    table.ForeignKey(
                        name: "FK_AlarmDetail_AlarmMaster_AlarmId",
                        column: x => x.AlarmId,
                        principalTable: "AlarmMaster",
                        principalColumn: "AlarmId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDetail_AlarmId",
                table: "AlarmDetail",
                column: "AlarmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmDetail");

            migrationBuilder.DropTable(
                name: "AlarmMaster");
        }
    }
}
