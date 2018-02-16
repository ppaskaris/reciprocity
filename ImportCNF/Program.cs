using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using FastMember;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImportCNF
{
    class Program
    {
        private const int CommandTimeout = 1200;

        private static Configuration CsvReaderConfiguration = new Configuration
        {
            PrepareHeaderForMatch = PrepareHeaderForMatch,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes
        };

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} \"connectionString\" \"./path/to/cnf-fcen-csv.zip\"");
                Environment.Exit(1);
            }

            var connectionString = args[0];
            Console.Error.WriteLine("Deleting existing data...");
            TruncateTable("reciprocity.CNF_NutrientAmount");
            TruncateTable("reciprocity.CNF_NutrientName");
            TruncateTable("reciprocity.CNF_Unit");
            TruncateTable("reciprocity.CNF_ConversionFactor");
            TruncateTable("reciprocity.CNF_MeasureName");
            TruncateTable("reciprocity.CNF_FoodName");

            var pathToCsv = Path.GetFullPath(args[1]);
            using (var archive = ZipFile.OpenRead(pathToCsv))
            {
                var entryByName = new Dictionary<string, ZipArchiveEntry>();
                foreach (var entry in archive.Entries)
                {
                    entryByName[entry.Name] = entry;
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine("Importing new data...");
                ImportEntry<FoodNameRecord>(
                    entryByName["FOOD NAME.csv"],
                    "reciprocity.CNF_FoodName",
                    FoodNameRecord.Members);
                ImportEntry<MeasureNameRecord>(
                    entryByName["MEASURE NAME.csv"],
                    "reciprocity.CNF_MeasureName",
                    MeasureNameRecord.Members);
                ImportEntry<ConversionFactorRecord>(
                    entryByName["CONVERSION FACTOR.csv"],
                    "reciprocity.CNF_ConversionFactor",
                    ConversionFactorRecord.Members);
                ImportEntry<NutrientNameRecord>(
                    entryByName["NUTRIENT NAME.csv"],
                    "reciprocity.CNF_NutrientName",
                    NutrientNameRecord.Members);
                ImportEntry<NutrientAmountRecord>(
                    entryByName["NUTRIENT AMOUNT.csv"],
                    "reciprocity.CNF_NutrientAmount",
                    NutrientAmountRecord.Members);
            }

            Console.Error.WriteLine();
            Console.Error.WriteLine("Removing unnecessary records...");
            Console.Error.WriteLine("  Removing measures for \"100g\"");
            Execute(
                @"
                DELETE
                FROM reciprocity.CNF_ConversionFactor
                WHERE CNF_ConversionFactor.MeasureId IN
                    (SELECT MeasureId
                     FROM reciprocity.CNF_MeasureName
                     WHERE MeasureDescription = '100g');

                DELETE
                FROM reciprocity.CNF_MeasureName
                WHERE MeasureDescription = '100g';
                ");

            Console.Error.WriteLine();
            Console.Error.WriteLine("Populating \"CNF_Unit\"...");
            PopulateUnitConversionTable();

            Console.Error.WriteLine();
            Console.Error.WriteLine("Populating full-text search indexes...");
            PopulateFullTextIndex("reciprocity.CNF_FoodName");

            Console.Error.WriteLine();
            Console.Error.WriteLine("Import complete. Press return key to continue...");
            Console.ReadLine();

            void TruncateTable(string tableName)
            {
                Console.Error.WriteLine($"  Deleting from \"{tableName}\"");
                Execute($"DELETE FROM {tableName};");
            }

            void PopulateFullTextIndex(string tableName)
            {
                Console.Error.WriteLine($"  Populating full-text index on \"{tableName}\"");
                Execute($"ALTER FULLTEXT INDEX ON {tableName} START FULL POPULATION;");
            }

            void Execute(string queryText)
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Execute(queryText, commandTimeout: CommandTimeout);
                }
            }

            void ImportEntry<TRecord>(ZipArchiveEntry entry, string tableName, string[] members)
            {
                using (var stream = entry.Open())
                using (var streamReader = new StreamReader(stream))
                using (var csvReader = new CsvReader(streamReader, CsvReaderConfiguration))
                {
                    var records = csvReader.GetRecords<TRecord>();
                    using (var dataReader = ObjectReader.Create(records, members))
                    using (var bulkCopy = new SqlBulkCopy(connectionString))
                    {
                        Console.Error.WriteLine($"  Inserting into \"{tableName}\"");
                        bulkCopy.BulkCopyTimeout = CommandTimeout;
                        bulkCopy.DestinationTableName = tableName;
                        bulkCopy.WriteToServer(dataReader);
                    }
                }
            }

            void PopulateUnitConversionTable()
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    Console.Error.WriteLine("  Reading from \"reciprocity.CNF_MeasureName\"");
                    var measures = connection.Query<MeasureNameRecord>(
                        "SELECT MeasureId, MeasureDescription FROM reciprocity.CNF_MeasureName;");
                    Console.Error.WriteLine("  Creating mapping \"reciprocity.CNF_MeasureName\" <-> \"reciprocity.CNF_Unit\"");
                    var units = measures
                        .SelectMany(CreateUnitRecords)
                        .ToList();
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    using (var dataReader = ObjectReader.Create(units, UnitRecord.Members))
                    using (var bulkCopy = new SqlBulkCopy(connection))
                    {
                        Console.Error.WriteLine("  Inserting into \"reciprocity.CNF_Unit\"");
                        bulkCopy.BulkCopyTimeout = CommandTimeout;
                        bulkCopy.DestinationTableName = "reciprocity.CNF_Unit";
                        bulkCopy.WriteToServer(dataReader);
                    }
                }
            }
        }

        #region DTOs

        class FoodNameRecord
        {
            public int FoodId { get; set; }
            public string FoodDescription { get; set; }

            public static string[] Members = {
                "FoodId",
                "FoodDescription"
            };
        }

        class MeasureNameRecord
        {
            public int MeasureId { get; set; }
            public string MeasureDescription { get; set; }

            public static string[] Members = {
                "MeasureId",
                "MeasureDescription"
            };
        }

        class ConversionFactorRecord
        {
            public int FoodId { get; set; }
            public int MeasureId { get; set; }
            public decimal ConversionFactorValue { get; set; }

            public static string[] Members = {
                "FoodId",
                "MeasureId",
                "ConversionFactorValue"
            };
        }

        class NutrientNameRecord
        {
            public int NutrientId { get; set; }
            public string NutrientSymbol { get; set; }
            public string NutrientUnit { get; set; }
            public string NutrientName { get; set; }
            public int NutrientDecimals { get; set; }

            public static string[] Members = {
                "NutrientId",
                "NutrientSymbol",
                "NutrientUnit",
                "NutrientName",
                "NutrientDecimals"
            };
        }

        class NutrientAmountRecord
        {
            public int FoodId { get; set; }
            public int NutrientId { get; set; }
            public decimal NutrientValue { get; set; }

            public static string[] Members = {
                "FoodId",
                "NutrientId",
                "NutrientValue"
            };
        }

        class UnitRecord
        {
            public int MeasureId { get; set; }
            public decimal Serving { get; set; }
            public string ServingType { get; set; }
            public string ServingCode { get; set; }
            public string Parenthetical { get; set; }

            public static string[] Members = {
                "MeasureId",
                "Serving",
                "ServingType",
                "ServingCode",
                "Parenthetical"
            };
        }

        private const RegexOptions RegexFlags =
            RegexOptions.Compiled | RegexOptions.IgnoreCase;

        private static readonly Regex UnitRegex =
            new Regex(@"^(?<Serving>\d+) ?(?<Unit>g|ml)\b(?:,? (?<Parenthetical>.+))?", RegexFlags);

        private static readonly Regex CountRegex =
            new Regex(@"^(?<Serving>\d+) (?<Name>[a-z]{3,}[^\(]*)(?<Parenthetical>\([^)]+\))?", RegexFlags);

        private static IEnumerable<UnitRecord> CreateUnitRecords(MeasureNameRecord measure)
        {
            Match match;

            match = UnitRegex.Match(measure.MeasureDescription);
            if (match.Success)
            {
                decimal serving = decimal.Parse(match.Groups["Serving"].Value);
                string unit = match.Groups["Unit"].Value;
                string parenthetical = ParseParenthetical(match.Groups["Parenthetical"]);
                string unitTypeCode = null;
                string unitCode = null;
                switch (unit)
                {
                    case "g":
                        unitTypeCode = "m";
                        unitCode = "g";
                        break;
                    case "ml":
                        unitTypeCode = "v";
                        unitCode = "ml";
                        break;
                }
                if (unitTypeCode != null && unitCode != null)
                {
                    yield return new UnitRecord
                    {
                        MeasureId = measure.MeasureId,
                        Serving = serving,
                        ServingType = unitTypeCode,
                        ServingCode = unitCode,
                        Parenthetical = parenthetical
                    };
                    yield break;
                }
            }

            match = CountRegex.Match(measure.MeasureDescription);
            if (match.Success)
            {
                decimal serving = decimal.Parse(match.Groups["Serving"].Value);
                if (serving > 1)
                {
                    string name = match.Groups["Name"].Value.Trim();
                    string parenthetical = ParseParenthetical(match.Groups["Parenthetical"]);
                    yield return new UnitRecord
                    {
                        MeasureId = measure.MeasureId,
                        Serving = serving,
                        ServingType = "q",
                        ServingCode = "pc",
                        Parenthetical = FormatNameAndParenthetical(name, parenthetical)
                    };
                    yield break;
                }
            }

            yield return new UnitRecord
            {
                MeasureId = measure.MeasureId,
                Serving = 1.00m,
                ServingType = "q",
                ServingCode = "pc",
                Parenthetical = measure.MeasureDescription
            };

            string FormatNameAndParenthetical(string name, string parenthetical)
            {
                string formatted = "1 " + name.Singularize(inputIsKnownToBePlural: false);
                if (parenthetical != null)
                {
                    formatted += " " + parenthetical;
                }
                return formatted;
            }
        }

        private static string ParseParenthetical(Group group)
        {
            if (group.Success && !string.IsNullOrWhiteSpace(group.Value))
            {
                return group.Value.Trim();
            }
            else
            {
                return null;
            }
        }

        #endregion

        private static string PrepareHeaderForMatch(string header)
        {
            return header.Trim().ToUpperInvariant();
        }
    }
}
