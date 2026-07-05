// File: DbContext/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using TextilePro.Core.Models;

namespace TextilePro.Core.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierContact> SupplierContacts { get; set; }
    public DbSet<ZDHCChemical> ZDHCChemicals { get; set; }
    public DbSet<SupplierChemical> SupplierChemicals { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }
    public DbSet<EvaluationAnswer> EvaluationAnswers { get; set; }
    public DbSet<EvaluationDocument> EvaluationDocuments { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK for SupplierChemical
        modelBuilder.Entity<SupplierChemical>()
            .HasKey(sc => new { sc.SupplierId, sc.ChemicalId });

        // Unique constraints
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<ZDHCChemical>()
            .HasIndex(c => c.ChemicalName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Seed
        SeedZDHCChemicals(modelBuilder);
        SeedUsers(modelBuilder);
    }

    private void SeedZDHCChemicals(ModelBuilder modelBuilder)
    {
        var chemicals = new List<ZDHCChemical>
        {
            new ZDHCChemical { Id = 1, Serial = "1", ChemicalName = "Acetic acid", CAS = "64-19-7", UsageDescription = "For neutralisation purposes", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 2, Serial = "2", ChemicalName = "Ammonia solution", CAS = "1336-21-6", UsageDescription = "pH adjustment", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 3, Serial = "3", ChemicalName = "Aluminium chloride hydroxide", CAS = "1327-41-9", UsageDescription = "Buffering agent, complexing agent", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 4, Serial = "4", ChemicalName = "Aluminium sulphate (Alum)", CAS = "10043-01-03", UsageDescription = "Tanning, water treatment chemical", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 5, Serial = "5", ChemicalName = "Ammonium bicarbonate", CAS = "1066-33-7", UsageDescription = "Neutralisation (leather processing)", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 6, Serial = "6", ChemicalName = "Ammonium carbonate", CAS = "506-87-6", UsageDescription = "Mordant in textile dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 7, Serial = "7", ChemicalName = "Ammonium chloride", CAS = "12125-02-09", UsageDescription = "Deliming (leather processing)", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 8, Serial = "8", ChemicalName = "Ammonium sulphate", CAS = "7783-20-2", UsageDescription = "Levelling, after-treatment agents", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 9, Serial = "9", ChemicalName = "Bis peroxide", CAS = "25155-25-3", UsageDescription = "Footwear-rubber", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 10, Serial = "10", ChemicalName = "Boric acid", CAS = "10043-35-3", UsageDescription = "Deliming (leather processing)", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 11, Serial = "11", ChemicalName = "Calcium carbonate", CAS = "471-34-1", UsageDescription = "Accessory & gear", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 12, Serial = "12", ChemicalName = "Calcium hydroxide", CAS = "12177-67-2", UsageDescription = "Hair removal/liming (leather processing), ETP", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 13, Serial = "13", ChemicalName = "Calcium hypochlorite", CAS = "7778-54-3", UsageDescription = "Bleaching, ETP, disinfecting chemicals", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 14, Serial = "14", ChemicalName = "Carboxymethyl cellulose (CMC)", CAS = "9004-32-4", UsageDescription = "Weaving-sizing agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 15, Serial = "15", ChemicalName = "Chrome alum", CAS = "10141-00-1", UsageDescription = "Tanning", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 16, Serial = "16", ChemicalName = "Chromium sulphate", CAS = "10101-53-8", UsageDescription = "Chrome tanning", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 17, Serial = "17", ChemicalName = "Citric acid anhydrous", CAS = "77-92-9", UsageDescription = "Neutralisation & buffering agent", RiskCategory = "Virgin and Non-Virgin" },
            new ZDHCChemical { Id = 18, Serial = "18", ChemicalName = "Citric acid monohydrate", CAS = "5949-29-1", UsageDescription = "Neutralisation & buffering agent", RiskCategory = "Virgin and Non-Virgin" },
            new ZDHCChemical { Id = 19, Serial = "19", ChemicalName = "Diammonium phosphate", CAS = "7783-28-0", UsageDescription = "Buffering agent in ETP", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 20, Serial = "20", ChemicalName = "Disodium phosphate", CAS = "7558-79-4", UsageDescription = "Catalyst in pigment printing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 21, Serial = "21", ChemicalName = "Dolomite", CAS = "16389-88-1", UsageDescription = "Neutralisation (leather processing)", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 22, Serial = "22", ChemicalName = "DTPA (Pentetic acid)", CAS = "67-43-6", UsageDescription = "Chelating agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 23, Serial = "23", ChemicalName = "EDTA (Edetic acid)", CAS = "60-00-4", UsageDescription = "Chelating agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 24, Serial = "24", ChemicalName = "Ferric chloride", CAS = "7705-08-0", UsageDescription = "ETP chemical", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 25, Serial = "25", ChemicalName = "Ferrous sulphate", CAS = "17375-41-6", UsageDescription = "ETP chemical", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 26, Serial = "26", ChemicalName = "Formic acid", CAS = "64-18-6", UsageDescription = "Neutralisation substitute of acetic acid", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 27, Serial = "27", ChemicalName = "Glucose", CAS = "50-99-7", UsageDescription = "Reducing agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 28, Serial = "28", ChemicalName = "Glycerine", CAS = "56-81-5", UsageDescription = "Lab reagent, additive in printing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 29, Serial = "29", ChemicalName = "Hydrochloric acid", CAS = "7647-01-0", UsageDescription = "Neutralisation substitute", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 30, Serial = "30", ChemicalName = "Hydrogen peroxide", CAS = "7722-84-1", UsageDescription = "Bleaching agent, oxidising agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 31, Serial = "31", ChemicalName = "Hydroxylamine sulphate", CAS = "10039-54-0", UsageDescription = "Buffering agent, hair removal", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 32, Serial = "32", ChemicalName = "Isopropyl palmitate", CAS = "142-91-6", UsageDescription = "Textile processing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 33, Serial = "33", ChemicalName = "Sodium 3-nitrobenzenesulphonate", CAS = "127-68-4", UsageDescription = "Antioxidant in dyeing and printing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 34, Serial = "34", ChemicalName = "Magnesium carbonate", CAS = "546-93-0", UsageDescription = "Neutralisation (leather processing)", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 35, Serial = "35", ChemicalName = "Magnesium chloride", CAS = "7786-30-3", UsageDescription = "ETP and textile complexing agent", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 36, Serial = "36", ChemicalName = "Magnesium hydroxide", CAS = "1309-42-8", UsageDescription = "Neutralisation (leather processing)", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 37, Serial = "37", ChemicalName = "Magnesium sulphate solution", CAS = "7487-88-9", UsageDescription = "Textile dyeing and leather tanning", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 38, Serial = "38", ChemicalName = "Nitric acid", CAS = "7697-37-2", UsageDescription = "Deionization ETP", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 39, Serial = "39", ChemicalName = "Oxalic acid", CAS = "114-62-7", UsageDescription = "Neutraliser for polyester dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 40, Serial = "40", ChemicalName = "Phosphoric acid", CAS = "7664-38-2", UsageDescription = "Buffering agent, neutralisation", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 41, Serial = "41", ChemicalName = "Polyaluminium chloride", CAS = "1327-41-9", UsageDescription = "Water and effluent treatment", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 42, Serial = "42", ChemicalName = "Polyethylene glycol", CAS = "25322-68-3", UsageDescription = "Humectant, anti-foaming", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 43, Serial = "43", ChemicalName = "Potassium alum", CAS = "10043-67-1", UsageDescription = "Water treatment", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 44, Serial = "44", ChemicalName = "Potassium dichromate", CAS = "7778-50-9", UsageDescription = "After chroming agent in dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 45, Serial = "45", ChemicalName = "Potassium dihydrogen phosphate", CAS = "7778-77-0", UsageDescription = "Buffering", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 46, Serial = "46", ChemicalName = "Potassium hydroxide", CAS = "1310-58-3", UsageDescription = "Lab reagent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 47, Serial = "47", ChemicalName = "Potassium permanganate", CAS = "7722-64-7", UsageDescription = "Denim fading, oxidising agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 48, Serial = "48", ChemicalName = "Silicon dioxide", CAS = "112926-00-8", UsageDescription = "Footwear-rubber", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 49, Serial = "49", ChemicalName = "Sodium acetate", CAS = "127-09-03", UsageDescription = "Buffer in dyeing", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 50, Serial = "50", ChemicalName = "Sodium acetate trihydrate", CAS = "6131-90-4", UsageDescription = "Buffer in dyeing", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 51, Serial = "51", ChemicalName = "Sodium alginate", CAS = "9005-38-3", UsageDescription = "Anti-migration in dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 52, Serial = "52", ChemicalName = "Sodium bicarbonate", CAS = "144-55-8", UsageDescription = "Printing, neutralisation, buffer", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 53, Serial = "53", ChemicalName = "Sodium carbonate", CAS = "497-19-8", UsageDescription = "Printing, neutralisation, buffer", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 54, Serial = "54", ChemicalName = "Sodium carbonate monohydrate", CAS = "5968-11-6", UsageDescription = "Neutralisation, buffering agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 55, Serial = "55", ChemicalName = "Sodium carbonate decahydrate", CAS = "6132-02-1", UsageDescription = "Dyeing agent for pH regulation", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 56, Serial = "56", ChemicalName = "Sodium chloride", CAS = "7647-14-5", UsageDescription = "Dyeing, bleaching, whitening", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 57, Serial = "57", ChemicalName = "Sodium citrate", CAS = "6132-04-3", UsageDescription = "WTP membrane cleaning", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 58, Serial = "58", ChemicalName = "Sodium dichromate", CAS = "7789-12-0", UsageDescription = "After chroming agent in dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 59, Serial = "59", ChemicalName = "Sodium formate", CAS = "141-53-7", UsageDescription = "Pickling (leather processing)", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 60, Serial = "60", ChemicalName = "Sodium hydrosulfite solid", CAS = "7775-14-6", UsageDescription = "Discharge printing, ETP", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 61, Serial = "61", ChemicalName = "Sodium hydrosulfite solution", CAS = "7775-14-6", UsageDescription = "Dyeing, denim dyeing", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 62, Serial = "62", ChemicalName = "Sodium hydrosulphide", CAS = "16721-80-5", UsageDescription = "Hair removal (leather processing)", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 63, Serial = "63", ChemicalName = "Sodium hydroxide pellets", CAS = "1310-73-2", UsageDescription = "Pre treatment & fixation", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 64, Serial = "64", ChemicalName = "Sodium hydroxide solution", CAS = "1310-73-2", UsageDescription = "Pre treatment, mercerising", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 65, Serial = "65", ChemicalName = "Sodium hypochlorite", CAS = "7681-52-9", UsageDescription = "Oxidising agent, cleaning", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 66, Serial = "66", ChemicalName = "Sodium lauryl sulphate", CAS = "151-21-3", UsageDescription = "Washing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 67, Serial = "67", ChemicalName = "Sodium metabisulfite", CAS = "7681-57-4", UsageDescription = "Anti oxidant", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 68, Serial = "68", ChemicalName = "Sodium metasilicate", CAS = "6834-92-0", UsageDescription = "Alkali in CPB dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 69, Serial = "69", ChemicalName = "Sodium nitrate", CAS = "7631-99-4", UsageDescription = "Textile enamel agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 70, Serial = "70", ChemicalName = "Sodium nitrite", CAS = "7632-00-0", UsageDescription = "Neutraliser and buffer in tanning", RiskCategory = "Not listed" },
            new ZDHCChemical { Id = 71, Serial = "71", ChemicalName = "Sodium perborate", CAS = "10486-00-7", UsageDescription = "Oxidising agent, filler", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 72, Serial = "72", ChemicalName = "Sodium percarbonate", CAS = "15630-89-4", UsageDescription = "Oxidising agent, dye fixing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 73, Serial = "73", ChemicalName = "Sodium persulfate", CAS = "7775-27-1", UsageDescription = "Bleaching agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 74, Serial = "74", ChemicalName = "Sodium polyphosphates", CAS = "68915-31-1", UsageDescription = "Buffer and alkali releasing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 75, Serial = "75", ChemicalName = "Sodium silicate", CAS = "1344-09-08", UsageDescription = "Silicate dyeing, fixing agent", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 76, Serial = "76", ChemicalName = "Sodium sulphate", CAS = "7757-82-6", UsageDescription = "Dyeing, ETP", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 77, Serial = "77", ChemicalName = "Sodium sulphide", CAS = "1313-82-2", UsageDescription = "Sulphur dyeing", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 78, Serial = "78", ChemicalName = "Sodium sulphite", CAS = "7757-83-7", UsageDescription = "Anti oxidant", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 79, Serial = "79", ChemicalName = "Sodium thiosulfate anhydrous", CAS = "7772-98-7", UsageDescription = "Lab reagent, print film", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 80, Serial = "80", ChemicalName = "Sodium thiosulfate solution", CAS = "10102-17-7", UsageDescription = "Lab reagent, print film", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 81, Serial = "81", ChemicalName = "Starch", CAS = "65996-63-6", UsageDescription = "Finishing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 82, Serial = "82", ChemicalName = "Stearic acid", CAS = "57-11-4", UsageDescription = "Textile, footwear-rubber", RiskCategory = "Not listed" },
            new ZDHCChemical { Id = 83, Serial = "83", ChemicalName = "Sulphuric acid", CAS = "7664-93-9", UsageDescription = "ETP, carbonising", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 84, Serial = "84", ChemicalName = "Thio urea dioxide", CAS = "1758-73-2", UsageDescription = "Reducing agent for dyeing", RiskCategory = "Virgin" },
            new ZDHCChemical { Id = 85, Serial = "85", ChemicalName = "Trisodium phosphate", CAS = "7601-54-9", UsageDescription = "Dyeing", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 86, Serial = "86", ChemicalName = "Urea", CAS = "57-13-6", UsageDescription = "Printing humectant", RiskCategory = "Not listed" },
            new ZDHCChemical { Id = 87, Serial = "87", ChemicalName = "Zinc sulphate", CAS = "7446-20-0", UsageDescription = "Mordant in printing", RiskCategory = "Non-Virgin" },
            new ZDHCChemical { Id = 88, Serial = "88", ChemicalName = "Zinc carbonate", CAS = "51839-25-9", UsageDescription = "Footwear-rubber", RiskCategory = "Non-Virgin" }
        };

        modelBuilder.Entity<ZDHCChemical>().HasData(chemicals);
    }

    private void SeedUsers(ModelBuilder modelBuilder)
    {
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@2025");
        var userHash = BCrypt.Net.BCrypt.HashPassword("User@2025");

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "Admin", PasswordHash = adminHash, IsAdmin = true },
            new User { Id = 2, Username = "User", PasswordHash = userHash, IsAdmin = false }
        );
    }
}