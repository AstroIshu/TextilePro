using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TextilePro.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    TableAffected = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZDHCChemicals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Serial = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ChemicalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CAS = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UsageDescription = table.Column<string>(type: "TEXT", nullable: true),
                    RiskCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZDHCChemicals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SelfDeclarationStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evaluations_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierContacts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChemicalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Volume = table.Column<decimal>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchaseMonth = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inventories_ZDHCChemicals_ChemicalId",
                        column: x => x.ChemicalId,
                        principalTable: "ZDHCChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierChemicals",
                columns: table => new
                {
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChemicalId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierChemicals", x => new { x.SupplierId, x.ChemicalId });
                    table.ForeignKey(
                        name: "FK_SupplierChemicals_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierChemicals_ZDHCChemicals_ChemicalId",
                        column: x => x.ChemicalId,
                        principalTable: "ZDHCChemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EvaluationId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedScore = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationAnswers_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EvaluationId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationDocuments_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "IsAdmin", "PasswordHash", "Username" },
                values: new object[,]
                {
                    { 1, true, "$2a$11$HT2tNH2XtgOcHRek0/vuuejbVnJOnYLcjmLglE7jOmO2ZBOQ7kIsC", "Admin" },
                    { 2, false, "$2a$11$flNTNcO6HTWl0wA0H8Ytnu7FYhMEJLo1JoxJ.1mfAbJ3PKh2fjiZC", "User" }
                });

            migrationBuilder.InsertData(
                table: "ZDHCChemicals",
                columns: new[] { "Id", "CAS", "ChemicalName", "RiskCategory", "Serial", "UsageDescription" },
                values: new object[,]
                {
                    { 1, "64-19-7", "Acetic acid", "Non-Virgin", "1", "For neutralisation purposes" },
                    { 2, "1336-21-6", "Ammonia solution", "Virgin", "2", "pH adjustment" },
                    { 3, "1327-41-9", "Aluminium chloride hydroxide", "Non-Virgin", "3", "Buffering agent, complexing agent" },
                    { 4, "10043-01-03", "Aluminium sulphate (Alum)", "Virgin", "4", "Tanning, water treatment chemical" },
                    { 5, "1066-33-7", "Ammonium bicarbonate", "Virgin", "5", "Neutralisation (leather processing)" },
                    { 6, "506-87-6", "Ammonium carbonate", "Virgin", "6", "Mordant in textile dyeing" },
                    { 7, "12125-02-09", "Ammonium chloride", "Virgin", "7", "Deliming (leather processing)" },
                    { 8, "7783-20-2", "Ammonium sulphate", "Virgin", "8", "Levelling, after-treatment agents" },
                    { 9, "25155-25-3", "Bis peroxide", "Virgin", "9", "Footwear-rubber" },
                    { 10, "10043-35-3", "Boric acid", "Virgin", "10", "Deliming (leather processing)" },
                    { 11, "471-34-1", "Calcium carbonate", "Non-Virgin", "11", "Accessory & gear" },
                    { 12, "12177-67-2", "Calcium hydroxide", "Non-Virgin", "12", "Hair removal/liming (leather processing), ETP" },
                    { 13, "7778-54-3", "Calcium hypochlorite", "Virgin", "13", "Bleaching, ETP, disinfecting chemicals" },
                    { 14, "9004-32-4", "Carboxymethyl cellulose (CMC)", "Virgin", "14", "Weaving-sizing agent" },
                    { 15, "10141-00-1", "Chrome alum", "Non-Virgin", "15", "Tanning" },
                    { 16, "10101-53-8", "Chromium sulphate", "Non-Virgin", "16", "Chrome tanning" },
                    { 17, "77-92-9", "Citric acid anhydrous", "Virgin and Non-Virgin", "17", "Neutralisation & buffering agent" },
                    { 18, "5949-29-1", "Citric acid monohydrate", "Virgin and Non-Virgin", "18", "Neutralisation & buffering agent" },
                    { 19, "7783-28-0", "Diammonium phosphate", "Virgin", "19", "Buffering agent in ETP" },
                    { 20, "7558-79-4", "Disodium phosphate", "Virgin", "20", "Catalyst in pigment printing" },
                    { 21, "16389-88-1", "Dolomite", "Virgin", "21", "Neutralisation (leather processing)" },
                    { 22, "67-43-6", "DTPA (Pentetic acid)", "Virgin", "22", "Chelating agent" },
                    { 23, "60-00-4", "EDTA (Edetic acid)", "Virgin", "23", "Chelating agent" },
                    { 24, "7705-08-0", "Ferric chloride", "Non-Virgin", "24", "ETP chemical" },
                    { 25, "17375-41-6", "Ferrous sulphate", "Non-Virgin", "25", "ETP chemical" },
                    { 26, "64-18-6", "Formic acid", "Non-Virgin", "26", "Neutralisation substitute of acetic acid" },
                    { 27, "50-99-7", "Glucose", "Virgin", "27", "Reducing agent" },
                    { 28, "56-81-5", "Glycerine", "Virgin", "28", "Lab reagent, additive in printing" },
                    { 29, "7647-01-0", "Hydrochloric acid", "Non-Virgin", "29", "Neutralisation substitute" },
                    { 30, "7722-84-1", "Hydrogen peroxide", "Virgin", "30", "Bleaching agent, oxidising agent" },
                    { 31, "10039-54-0", "Hydroxylamine sulphate", "Virgin", "31", "Buffering agent, hair removal" },
                    { 32, "142-91-6", "Isopropyl palmitate", "Virgin", "32", "Textile processing" },
                    { 33, "127-68-4", "Sodium 3-nitrobenzenesulphonate", "Virgin", "33", "Antioxidant in dyeing and printing" },
                    { 34, "546-93-0", "Magnesium carbonate", "Non-Virgin", "34", "Neutralisation (leather processing)" },
                    { 35, "7786-30-3", "Magnesium chloride", "Non-Virgin", "35", "ETP and textile complexing agent" },
                    { 36, "1309-42-8", "Magnesium hydroxide", "Non-Virgin", "36", "Neutralisation (leather processing)" },
                    { 37, "7487-88-9", "Magnesium sulphate solution", "Non-Virgin", "37", "Textile dyeing and leather tanning" },
                    { 38, "7697-37-2", "Nitric acid", "Non-Virgin", "38", "Deionization ETP" },
                    { 39, "114-62-7", "Oxalic acid", "Virgin", "39", "Neutraliser for polyester dyeing" },
                    { 40, "7664-38-2", "Phosphoric acid", "Virgin", "40", "Buffering agent, neutralisation" },
                    { 41, "1327-41-9", "Polyaluminium chloride", "Non-Virgin", "41", "Water and effluent treatment" },
                    { 42, "25322-68-3", "Polyethylene glycol", "Virgin", "42", "Humectant, anti-foaming" },
                    { 43, "10043-67-1", "Potassium alum", "Virgin", "43", "Water treatment" },
                    { 44, "7778-50-9", "Potassium dichromate", "Virgin", "44", "After chroming agent in dyeing" },
                    { 45, "7778-77-0", "Potassium dihydrogen phosphate", "Non-Virgin", "45", "Buffering" },
                    { 46, "1310-58-3", "Potassium hydroxide", "Virgin", "46", "Lab reagent" },
                    { 47, "7722-64-7", "Potassium permanganate", "Virgin", "47", "Denim fading, oxidising agent" },
                    { 48, "112926-00-8", "Silicon dioxide", "Virgin", "48", "Footwear-rubber" },
                    { 49, "127-09-03", "Sodium acetate", "Non-Virgin", "49", "Buffer in dyeing" },
                    { 50, "6131-90-4", "Sodium acetate trihydrate", "Non-Virgin", "50", "Buffer in dyeing" },
                    { 51, "9005-38-3", "Sodium alginate", "Virgin", "51", "Anti-migration in dyeing" },
                    { 52, "144-55-8", "Sodium bicarbonate", "Virgin", "52", "Printing, neutralisation, buffer" },
                    { 53, "497-19-8", "Sodium carbonate", "Virgin", "53", "Printing, neutralisation, buffer" },
                    { 54, "5968-11-6", "Sodium carbonate monohydrate", "Virgin", "54", "Neutralisation, buffering agent" },
                    { 55, "6132-02-1", "Sodium carbonate decahydrate", "Virgin", "55", "Dyeing agent for pH regulation" },
                    { 56, "7647-14-5", "Sodium chloride", "Non-Virgin", "56", "Dyeing, bleaching, whitening" },
                    { 57, "6132-04-3", "Sodium citrate", "Non-Virgin", "57", "WTP membrane cleaning" },
                    { 58, "7789-12-0", "Sodium dichromate", "Virgin", "58", "After chroming agent in dyeing" },
                    { 59, "141-53-7", "Sodium formate", "Virgin", "59", "Pickling (leather processing)" },
                    { 60, "7775-14-6", "Sodium hydrosulfite solid", "Virgin", "60", "Discharge printing, ETP" },
                    { 61, "7775-14-6", "Sodium hydrosulfite solution", "Non-Virgin", "61", "Dyeing, denim dyeing" },
                    { 62, "16721-80-5", "Sodium hydrosulphide", "Virgin", "62", "Hair removal (leather processing)" },
                    { 63, "1310-73-2", "Sodium hydroxide pellets", "Non-Virgin", "63", "Pre treatment & fixation" },
                    { 64, "1310-73-2", "Sodium hydroxide solution", "Non-Virgin", "64", "Pre treatment, mercerising" },
                    { 65, "7681-52-9", "Sodium hypochlorite", "Virgin", "65", "Oxidising agent, cleaning" },
                    { 66, "151-21-3", "Sodium lauryl sulphate", "Virgin", "66", "Washing" },
                    { 67, "7681-57-4", "Sodium metabisulfite", "Virgin", "67", "Anti oxidant" },
                    { 68, "6834-92-0", "Sodium metasilicate", "Virgin", "68", "Alkali in CPB dyeing" },
                    { 69, "7631-99-4", "Sodium nitrate", "Virgin", "69", "Textile enamel agent" },
                    { 70, "7632-00-0", "Sodium nitrite", "Not listed", "70", "Neutraliser and buffer in tanning" },
                    { 71, "10486-00-7", "Sodium perborate", "Virgin", "71", "Oxidising agent, filler" },
                    { 72, "15630-89-4", "Sodium percarbonate", "Virgin", "72", "Oxidising agent, dye fixing" },
                    { 73, "7775-27-1", "Sodium persulfate", "Virgin", "73", "Bleaching agent" },
                    { 74, "68915-31-1", "Sodium polyphosphates", "Virgin", "74", "Buffer and alkali releasing" },
                    { 75, "1344-09-08", "Sodium silicate", "Virgin", "75", "Silicate dyeing, fixing agent" },
                    { 76, "7757-82-6", "Sodium sulphate", "Non-Virgin", "76", "Dyeing, ETP" },
                    { 77, "1313-82-2", "Sodium sulphide", "Non-Virgin", "77", "Sulphur dyeing" },
                    { 78, "7757-83-7", "Sodium sulphite", "Non-Virgin", "78", "Anti oxidant" },
                    { 79, "7772-98-7", "Sodium thiosulfate anhydrous", "Virgin", "79", "Lab reagent, print film" },
                    { 80, "10102-17-7", "Sodium thiosulfate solution", "Virgin", "80", "Lab reagent, print film" },
                    { 81, "65996-63-6", "Starch", "Virgin", "81", "Finishing" },
                    { 82, "57-11-4", "Stearic acid", "Not listed", "82", "Textile, footwear-rubber" },
                    { 83, "7664-93-9", "Sulphuric acid", "Non-Virgin", "83", "ETP, carbonising" },
                    { 84, "1758-73-2", "Thio urea dioxide", "Virgin", "84", "Reducing agent for dyeing" },
                    { 85, "7601-54-9", "Trisodium phosphate", "Non-Virgin", "85", "Dyeing" },
                    { 86, "57-13-6", "Urea", "Not listed", "86", "Printing humectant" },
                    { 87, "7446-20-0", "Zinc sulphate", "Non-Virgin", "87", "Mordant in printing" },
                    { 88, "51839-25-9", "Zinc carbonate", "Non-Virgin", "88", "Footwear-rubber" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationAnswers_EvaluationId",
                table: "EvaluationAnswers",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationDocuments_EvaluationId",
                table: "EvaluationDocuments",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_SupplierId",
                table: "Evaluations",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ChemicalId",
                table: "Inventories",
                column: "ChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_SupplierId",
                table: "Inventories",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierChemicals_ChemicalId",
                table: "SupplierChemicals",
                column: "ChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_SupplierId",
                table: "SupplierContacts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZDHCChemicals_ChemicalName",
                table: "ZDHCChemicals",
                column: "ChemicalName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "EvaluationAnswers");

            migrationBuilder.DropTable(
                name: "EvaluationDocuments");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "SupplierChemicals");

            migrationBuilder.DropTable(
                name: "SupplierContacts");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "ZDHCChemicals");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
