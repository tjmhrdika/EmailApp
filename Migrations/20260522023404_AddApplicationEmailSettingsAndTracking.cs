using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailApp.Migrations
{
    public partial class AddApplicationEmailSettingsAndTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmDetails");

            migrationBuilder.DropTable(
                name: "AlarmMasters");

            migrationBuilder.AddColumn<Guid>(
                name: "EmailGroupId",
                table: "Emails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlarmEmailTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmDetailId = table.Column<int>(type: "int", nullable: false),
                    AlarmId = table.Column<int>(type: "int", nullable: false),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailRecipients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEmailTracking", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetSmtp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Host = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetSmtp", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SetSmtp",
                columns: new[] { "Id", "FromEmail", "Host", "Pass", "Port", "User" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "", "", "", 0, "" });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_EmailGroupId",
                table: "Emails",
                column: "EmailGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEmailTracking_AlarmDetailId",
                table: "AlarmEmailTracking",
                column: "AlarmDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_Name",
                table: "EmailGroups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_EmailGroups_EmailGroupId",
                table: "Emails",
                column: "EmailGroupId",
                principalTable: "EmailGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Emails_EmailGroups_EmailGroupId",
                table: "Emails");

            migrationBuilder.DropTable(
                name: "AlarmEmailTracking");

            migrationBuilder.DropTable(
                name: "EmailGroups");

            migrationBuilder.DropTable(
                name: "SetSmtp");

            migrationBuilder.DropIndex(
                name: "IX_Emails_EmailGroupId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "EmailGroupId",
                table: "Emails");

            migrationBuilder.CreateTable(
                name: "AlarmMasters",
                columns: table => new
                {
                    AlarmId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CauseId = table.Column<int>(type: "int", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmMasters", x => x.AlarmId);
                });

            migrationBuilder.CreateTable(
                name: "AlarmDetails",
                columns: table => new
                {
                    AlarmDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmMasterAlarmId = table.Column<int>(type: "int", nullable: false),
                    AlarmId = table.Column<int>(type: "int", nullable: false),
                    AlarmState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentId = table.Column<int>(type: "int", nullable: true),
                    EventStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmDetails", x => x.AlarmDetailId);
                    table.ForeignKey(
                        name: "FK_AlarmDetails_AlarmMasters_AlarmMasterAlarmId",
                        column: x => x.AlarmMasterAlarmId,
                        principalTable: "AlarmMasters",
                        principalColumn: "AlarmId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDetails_AlarmMasterAlarmId",
                table: "AlarmDetails",
                column: "AlarmMasterAlarmId");
        }
    }
}
