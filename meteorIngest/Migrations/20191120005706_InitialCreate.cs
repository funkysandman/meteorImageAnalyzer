using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace meteorIngest.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoundingBox",
                columns: table => new
                {
                    boundingBoxId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    xmin = table.Column<int>(nullable: false),
                    ymin = table.Column<int>(nullable: false),
                    xmax = table.Column<int>(nullable: false),
                    ymax = table.Column<int>(nullable: false),
                    skyObjectID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoundingBox", x => x.boundingBoxId);
                });

            migrationBuilder.CreateTable(
                name: "SkyImages",
                columns: table => new
                {
                    skyImageId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    filename = table.Column<string>(nullable: true),
                    camera = table.Column<string>(nullable: true),
                    width = table.Column<int>(nullable: false),
                    height = table.Column<int>(nullable: false),
                    date = table.Column<DateTime>(nullable: false),
                    imageSet = table.Column<int>(nullable: false),
                    rank = table.Column<int>(nullable: false),
                    selectedForTraining = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyImages", x => x.skyImageId);
                });

            migrationBuilder.CreateTable(
                name: "ImageData",
                columns: table => new
                {
                    imageID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    imageData = table.Column<string>(nullable: true),
                    skyImageId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageData", x => x.imageID);
                    table.ForeignKey(
                        name: "FK_ImageData_SkyImages_skyImageId",
                        column: x => x.skyImageId,
                        principalTable: "SkyImages",
                        principalColumn: "skyImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkyObjectDetection",
                columns: table => new
                {
                    skyObjectID = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    skyObjectClass = table.Column<string>(nullable: true),
                    bboxboundingBoxId = table.Column<int>(nullable: true),
                    score = table.Column<decimal>(nullable: false),
                    skyImageId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyObjectDetection", x => x.skyObjectID);
                    table.ForeignKey(
                        name: "FK_SkyObjectDetection_BoundingBox_bboxboundingBoxId",
                        column: x => x.bboxboundingBoxId,
                        principalTable: "BoundingBox",
                        principalColumn: "boundingBoxId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SkyObjectDetection_SkyImages_skyImageId",
                        column: x => x.skyImageId,
                        principalTable: "SkyImages",
                        principalColumn: "skyImageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageData_skyImageId",
                table: "ImageData",
                column: "skyImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkyObjectDetection_bboxboundingBoxId",
                table: "SkyObjectDetection",
                column: "bboxboundingBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyObjectDetection_skyImageId",
                table: "SkyObjectDetection",
                column: "skyImageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageData");

            migrationBuilder.DropTable(
                name: "SkyObjectDetection");

            migrationBuilder.DropTable(
                name: "BoundingBox");

            migrationBuilder.DropTable(
                name: "SkyImages");
        }
    }
}
