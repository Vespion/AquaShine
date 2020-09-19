using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AquaShine.ApiHub.Data.Migrations
{
    public partial class InitalMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    City = table.Column<string>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    Region = table.Column<string>(nullable: true),
                    PostalCode = table.Column<string>(nullable: true),
                    Address1 = table.Column<string>(nullable: true),
                    Address2 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VerifyingImgUrl = table.Column<string>(nullable: true),
                    DisplayImgUrl = table.Column<string>(nullable: true),
                    TimeToComplete = table.Column<TimeSpan>(nullable: true),
                    Verified = table.Column<bool>(nullable: false),
                    Locked = table.Column<bool>(nullable: false),
                    Show = table.Column<bool>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entrants",
                columns: table => new
                {
                    RowKey = table.Column<string>(nullable: false),
                    SoftDelete = table.Column<bool>(nullable: false),
                    EventbriteId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    SubmissionId = table.Column<int>(nullable: true),
                    Email = table.Column<string>(nullable: false),
                    AddressId = table.Column<int>(nullable: false),
                    BioGender = table.Column<int>(nullable: false),
                    PartitionKey = table.Column<string>(nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false),
                    ETag = table.Column<string>(nullable: true),
                    EntrantId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrants", x => x.RowKey);
                    table.ForeignKey(
                        name: "FK_Entrants_Address_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entrants_Submissions_EntrantId",
                        column: x => x.EntrantId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entrants_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entrants_AddressId",
                table: "Entrants",
                column: "AddressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entrants_EntrantId",
                table: "Entrants",
                column: "EntrantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entrants_EventbriteId",
                table: "Entrants",
                column: "EventbriteId");

            migrationBuilder.CreateIndex(
                name: "IX_Entrants_SubmissionId",
                table: "Entrants",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Locked",
                table: "Submissions",
                column: "Locked");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TimeToComplete",
                table: "Submissions",
                column: "TimeToComplete");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Verified",
                table: "Submissions",
                column: "Verified");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entrants");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}
