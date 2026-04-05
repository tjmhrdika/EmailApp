using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "AlarmMasters",
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
                    table.PrimaryKey("PK_AlarmMasters", x => x.AlarmId);
                });

            migrationBuilder.CreateTable(
                name: "EmailGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlarmDetails",
                columns: table => new
                {
                    AlarmDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlarmId = table.Column<int>(type: "int", nullable: false),
                    AlarmState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    OperatorNode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlarmMasterAlarmId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    EmailGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emails_EmailGroups_EmailGroupId",
                        column: x => x.EmailGroupId,
                        principalTable: "EmailGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SetSmtp",
                columns: new[] { "Id", "FromEmail", "Host", "Pass", "Port", "User" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "", "", "", 0, "" });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDetails_AlarmMasterAlarmId",
                table: "AlarmDetails",
                column: "AlarmMasterAlarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_EmailGroupId",
                table: "Emails",
                column: "EmailGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                table: "Groups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmDetails");

            migrationBuilder.DropTable(
                name: "AlarmEmailTracking");

            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "SetSmtp");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "AlarmMasters");

            migrationBuilder.DropTable(
                name: "EmailGroups");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
