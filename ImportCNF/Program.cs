using CsvHelper;
using CsvHelper.Configuration;
using FastMember;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;

namespace ImportCNF
{
    class Program
    {
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
            Console.Error.WriteLine("Populating full-text search indexes...");
            PopulateFullTextIndex("reciprocity.CNF_FoodName");

            Console.Error.WriteLine();
            Console.Error.WriteLine("Import complete. Press return key to continue...");
            Console.ReadLine();

            void TruncateTable(string tableName)
            {
                var queryText = $"DELETE FROM {tableName};";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(queryText, connection))
                    {
                        Console.Error.WriteLine($"  Deleting from \"{tableName}\"");
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                    }
                }
            }

            void PopulateFullTextIndex(string tableName)
            {
                var queryText = $"ALTER FULLTEXT INDEX ON {tableName} START FULL POPULATION;";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(queryText, connection))
                    {
                        Console.Error.WriteLine($"  Populating full-text index on \"{tableName}\"");
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                    }
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
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = tableName;
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

        #endregion

        private static string PrepareHeaderForMatch(string header)
        {
            return header.Trim().ToUpperInvariant();
        }
    }
}
