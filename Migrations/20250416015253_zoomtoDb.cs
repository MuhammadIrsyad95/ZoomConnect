using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoom.Migrations
{
    /// <inheritdoc />
    public partial class zoomtoDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Meeting",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: true, defaultValueSql: "(getdate())"),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true, defaultValueSql: "(getdate())"),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Agenda = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZoomID = table.Column<long>(type: "bigint", nullable: true),
                    ZoomAccount = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Passcode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    JoinUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Meeting__3214EC27DEC5C458", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ZoomAccount",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoomAccount", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meeting");

            migrationBuilder.DropTable(
                name: "ZoomAccount");
        }
    }
}
