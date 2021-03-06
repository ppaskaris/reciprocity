﻿using Dapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.SqlServer.Server;
using reciprocity.Data;
using reciprocity.Models.Book;
using reciprocity.Models.Home;
using reciprocity.Models.Recipe;
using reciprocity.SecurityTheatre;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace reciprocity.Services.Default
{
    public class DataService : IDataService
    {
        private readonly IConnectionFactory _connectionFactory;

        public DataService(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        #region Helpers

        private SqlConnection GetConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        #endregion

        async Task<BookKeyModel> IDataService.CreateBookAsync(string name)
        {
            var book = new BookModel
            {
                BookId = Guid.NewGuid(),
                Token = BearerToken.CreateRandom(),
                Name = name
            };
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    INSERT INTO Book (BookId, Token, [Name])
                    VALUES (@bookId, @token, @name);
                    ",
                    book);
            }
            return new BookKeyModel
            {
                BookId = book.BookId,
                Token = book.Token.ToString()
            };
        }

        async Task<BookModel> IDataService.GetBookAsync(Guid bookId)
        {
            using (var connection = GetConnection())
            {
                var book = await connection.QuerySingleOrDefaultAsync<BookModel>(
                    @"
                    SELECT BookId, Token, [Name]
                    FROM Book
                    WHERE BookId = @bookId;
                    ",
                    new { bookId });
                return book;
            }
        }

        async Task IDataService.RenameBookAsync(Guid bookId, string name)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    UPDATE Book
                    SET [Name] = @name
                    WHERE BookId = @bookId;
                    ",
                    new { bookId, name });
            }
        }

        async Task IDataService.DeleteBookAsync(Guid bookId)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    DELETE FROM Book
                    WHERE BookId = @bookId;
                    ",
                    new { bookId });
            }
        }

        async Task<RecipeKeyModel> IDataService.CreateRecipeAsync(Guid bookId, AddRecipeModel model)
        {
            var now = DateTime.UtcNow;
            var recipe = new RecipeModel
            {
                BookId = bookId,
                RecipeId = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                Servings = model.Servings,
                AddedAt = now,
                LastModifiedAt = now
            };
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    INSERT INTO BookRecipe (BookId, RecipeId, [Name], [Description], Servings, AddedAt, LastModifiedat)
                    VALUES (@bookId, @recipeId, @name, @description, @servings, @addedAt, @lastModifiedAt);
                    ",
                    recipe
                );
            }
            return new RecipeKeyModel
            {
                BookId = recipe.BookId,
                RecipeId = recipe.RecipeId
            };
        }

        async Task<RecipeModel> IDataService.GetRecipeAsync(Guid bookId, Guid recipeId)
        {
            using (var connection = GetConnection())
            {
                var recipe = await connection.QuerySingleOrDefaultAsync<RecipeModel>(
                    @"
                    SELECT BookId, RecipeId, [Name], [Description], Servings, AddedAt, LastModifiedAt
                    FROM BookRecipe
                    WHERE BookId = @bookId AND RecipeId = @recipeId;
                    ",
                    new { bookId, recipeId }
                );
                return recipe;
            }
        }

        async Task<BookViewModel> IDataService.GetBookViewAsync(Guid bookId)
        {
            const string queryText =
                @"
                SELECT BookId, [Name]
                FROM Book
                WHERE BookId = @bookId;

                SELECT
                    BookRecipe.RecipeId,
                    BookRecipe.[Name],
                    BookRecipe.[Description],
                    BookRecipe.Servings,
                    BookRecipeStatistics.CaloriesPerServing,
                    BookRecipeStatistics.ProteinPerServing,
                    BookRecipe.AddedAt,
                    BookRecipe.LastModifiedAt
                FROM BookRecipe
                LEFT JOIN BookRecipeStatistics
                    ON BookRecipeStatistics.BookId = BookRecipe.BookId
                    AND BookRecipeStatistics.RecipeId = BookRecipe.RecipeId
                WHERE BookRecipe.BookId = @bookId;
                ";
            var queryParams = new { bookId };
            using (var connection = GetConnection())
            using (var query = await connection.QueryMultipleAsync(queryText, queryParams))
            {
                var book = await query.ReadFirstAsync<BookModel>();
                var recipes = await query.ReadAsync<RecipeListItemViewModel>();
                return new BookViewModel
                {
                    Book = book,
                    Recipes = recipes
                };
            }
        }

        async Task IDataService.UpdateRecipeAsync(Guid bookId, Guid recipeId, EditRecipeModel model)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    UPDATE BookRecipe
                    SET
                        [Name] = @name,
                        [Description] = @description,
                        Servings = @servings,
                        LastModifiedAt = @now
                    WHERE BookId = @bookId AND RecipeId = @recipeId;
                    ",
                    new
                    {
                        bookId,
                        recipeId,
                        name = model.Name,
                        description = model.Description,
                        servings = model.Servings,
                        now = DateTime.UtcNow,
                    });
            }
        }

        async Task IDataService.DeleteRecipeAsync(Guid bookId, Guid recipeId)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    DELETE FROM BookRecipe
                    WHERE BookId = @bookId AND RecipeId = @recipeId;
                    ",
                    new { bookId, recipeId });
            }
        }

        #region GetUnitsAsync Helpers

        private class UnitTypeDto
        {
            public string UnitTypeCode { get; set; }
            public string Name { get; set; }
        }

        private class UnitDto
        {
            public string UnitTypeCode { get; set; }
            public string UnitCode { get; set; }
            public string Name { get; set; }
        }

        private static IEnumerable<SelectListItem> ToSelectListItems(
            IEnumerable<UnitTypeDto> unitTypes,
            IEnumerable<UnitDto> units)
        {
            var groups = unitTypes.ToDictionary(
                unitType => unitType.UnitTypeCode,
                unitType => new SelectListGroup { Name = unitType.Name });
            return units.Select(unit => new SelectListItem
            {
                Group = groups[unit.UnitTypeCode],
                Text = unit.Name,
                Value = $"{unit.UnitTypeCode},{unit.UnitCode}"
            });
        }

        #endregion

        async Task<IEnumerable<SelectListItem>> IDataService.GetUnitsAsync()
        {
            const string queryText =
                @"
                SELECT UnitTypeCode, [Name]
                FROM UnitType;

                SELECT Unit.UnitTypeCode, Unit.UnitCode, Unit.[Name]
                FROM Unit
                INNER JOIN UnitType
                    ON UnitType.UnitTypeCode = Unit.UnitTypeCode
                ORDER BY UnitType.SortOrder, Unit.ConversionRatio, Unit.[Name];
                ";
            using (var connection = GetConnection())
            using (var query = await connection.QueryMultipleAsync(queryText))
            {
                var groups = await query.ReadAsync<UnitTypeDto>();
                var units = await query.ReadAsync<UnitDto>();
                return ToSelectListItems(groups, units);
            }
        }

        async Task<IEnumerable<IngredientModel>> IDataService.GetIngredientsAsync(Guid bookId, Guid recipeId)
        {
            using (var connection = GetConnection())
            {
                return await connection.QueryAsync<IngredientModel>(
                    @"
                    SELECT
                        BookId,
                        RecipeId,
                        IngredientNo,
                        [Name],
                        Quantity,
                        QuantityType,
                        QuantityUnit,
                        Serving,
                        ServingType,
                        ServingUnit,
                        CaloriesPerServing,
                        ProteinPerServing
                    FROM BookRecipeIngredient
                    WHERE BookId = @bookId AND RecipeId = @recipeId
                    ORDER BY IngredientNo;
                    ",
                    new { bookId, recipeId });
            }
        }

        #region SaveIngredientsAsync Helpers

        private static readonly SqlMetaData[] BookRecipeIngredientMetaData =
        {
            new SqlMetaData("IngredientNo", SqlDbType.Int),
            new SqlMetaData("Name", SqlDbType.NVarChar, 255),
            new SqlMetaData("Quantity", SqlDbType.Decimal, 7, 2),
            new SqlMetaData("QuantityType", SqlDbType.Char, 1),
            new SqlMetaData("QuantityUnit", SqlDbType.VarChar, 3),
            new SqlMetaData("Serving", SqlDbType.Decimal, 7, 2),
            new SqlMetaData("ServingType", SqlDbType.Char, 1),
            new SqlMetaData("ServingUnit", SqlDbType.VarChar, 3),
            new SqlMetaData("CaloriesPerServing", SqlDbType.Decimal, 7, 2),
            new SqlMetaData("ProteinPerServing", SqlDbType.Decimal, 7, 2),
        };

        private class UnitKey
        {
            public string UnitTypeCode { get; set; }
            public string UnitCode { get; set; }

            public static UnitKey Parse(string typeAndCode)
            {
                // UnitTypeCode
                //     ↓
                //    "v,tsp"
                //       ↑↑↑
                //     UnitCode
                return new UnitKey
                {
                    UnitTypeCode = typeAndCode.Substring(0, 1),
                    UnitCode = typeAndCode.Substring(2)
                };
            }
        }

        private static SqlDataRecord CreateBookRecipeIngredientRecord(EditIngredientModel ingredient)
        {
            var dataRecord = new SqlDataRecord(BookRecipeIngredientMetaData);
            dataRecord.SetInt32(0, ingredient.IngredientNo.Value);
            dataRecord.SetString(1, ingredient.Name);
            dataRecord.SetDecimal(2, ingredient.Quantity.Value);
            var quantityUnit = UnitKey.Parse(ingredient.QuantityUnit);
            dataRecord.SetString(3, quantityUnit.UnitTypeCode);
            dataRecord.SetString(4, quantityUnit.UnitCode);
            dataRecord.SetDecimal(5, ingredient.Serving.Value);
            var servingUnit = UnitKey.Parse(ingredient.ServingUnit);
            dataRecord.SetString(6, servingUnit.UnitTypeCode);
            dataRecord.SetString(7, servingUnit.UnitCode);
            dataRecord.SetDecimal(8, ingredient.CaloriesPerServing.Value);
            dataRecord.SetDecimal(9, ingredient.ProteinPerServing.Value);
            return dataRecord;
        }

        #endregion

        async Task IDataService.SaveIngredientsAsync(Guid bookId, Guid recipeId, IEnumerable<EditIngredientModel> ingredients)
        {
            var now = DateTime.UtcNow;
            var ingredientRecords = ingredients
                .Select(CreateBookRecipeIngredientRecord)
                .ToList();
            using (var connection = GetConnection())
            {
                if (ingredientRecords.Count > 0)
                {

                    var ingredientTable = ingredientRecords
                        .AsTableValuedParameter("SaveBookRecipeIngredient");
                    await connection.ExecuteAsync(
                        @"
                        MERGE INTO BookRecipeIngredient target
                        USING @ingredientTable source
                             ON target.BookId = @bookId
                            AND target.RecipeId = @recipeId
                            AND target.IngredientNo = source.IngredientNo
                        WHEN MATCHED THEN
                            UPDATE SET
                                [Name] = source.[Name],
                                Quantity = source.Quantity,
                                QuantityType = source.QuantityType,
                                QuantityUnit = source.QuantityUnit,
                                Serving = source.Serving,
                                ServingType = source.ServingType,
                                ServingUnit = source.ServingUnit,
                                CaloriesPerServing = source.CaloriesPerServing,
                                ProteinPerServing = source.ProteinPerServing
                        WHEN NOT MATCHED BY TARGET THEN
                            INSERT (
                                BookId,
                                RecipeId,
                                IngredientNo,
                                [Name],
                                Quantity,
                                QuantityType,
                                QuantityUnit,
                                Serving,
                                ServingType,
                                ServingUnit,
                                CaloriesPerServing,
                                ProteinPerServing
                            ) VALUES (
                                @bookId,
                                @recipeId,
                                source.IngredientNo,
                                source.[Name],
                                source.Quantity,
                                source.QuantityType,
                                source.QuantityUnit,
                                source.Serving,
                                source.ServingType,
                                source.ServingUnit,
                                source.CaloriesPerServing,
                                source.ProteinPerServing
                            )
                        WHEN NOT MATCHED BY SOURCE
                            AND target.BookId = @bookId
                            AND target.RecipeId = @recipeId THEN
                                DELETE;

                        UPDATE BookRecipe
                        SET LastModifiedAt = @now
                        WHERE BookId = @bookId AND RecipeId = @recipeId;
                        ",
                        new
                        {
                            bookId,
                            recipeId,
                            ingredientTable,
                            now
                        });
                }
                else
                {
                    await connection.ExecuteAsync(
                        @"
                        DELETE FROM BookRecipeIngredient
                        WHERE BookId = @bookId AND RecipeID = @recipeId;

                        UPDATE BookRecipe
                        SET LastModifiedAt = @now
                        WHERE BookId = @bookId AND RecipeId = @recipeId;
                        ",
                        new
                        {
                            bookId,
                            recipeId,
                            now
                        });
                }
            }
        }

        async Task<(IEnumerable<IngredientViewModel>, int, decimal)> IDataService.GetRecipeViewComponentsAsync(Guid bookId, Guid recipeId)
        {
            const string queryText =
                @"
                SELECT
                    BookRecipeIngredient.[Name],
                    BookRecipeIngredient.Quantity,
                    Unit.[Name] AS UnitName,
                    Unit.Abbreviation AS UnitAbbreviation
                FROM BookRecipeIngredient
                INNER JOIN Unit
                        ON Unit.UnitTypeCode = BookRecipeIngredient.QuantityType
                    AND Unit.UnitCode = BookRecipeIngredient.QuantityUnit
                WHERE BookId = @bookId AND RecipeId = @recipeId
                ORDER BY IngredientNo;

                SELECT
                    CaloriesPerServing,
                    ProteinPerServing
                FROM BookRecipeStatistics
                WHERE BookId = @bookId AND RecipeId = @recipeId;
                ";
            var queryParams = new
            {
                bookId,
                recipeId
            };
            using (var connection = GetConnection())
            using (var query = await connection.QueryMultipleAsync(queryText, queryParams))
            {
                var ingredients = await query.ReadAsync<IngredientViewModel>();
                var (caloriesPerServing, proteinPerServing) = await query
                    .ReadFirstOrDefaultAsync<(int, decimal)>();
                return (ingredients, caloriesPerServing, proteinPerServing);
            }
        }

        #region GetSuggestionsAsync Helpers

        private string StripParenthetical(string value)
        {
            int stack = 0;
            bool flag = false;
            for (int i = value.Length - 1; i >= 0; --i)
            {
                char ch = value[i];
                if (ch == ')')
                {
                    ++stack;
                    flag = true;
                }
                else if (ch == '(')
                {
                    if (stack > 0)
                    {
                        --stack;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                if (flag && stack == 0)
                {
                    return value.Substring(0, i);
                }
            }
            if (stack > 0)
            {
                return string.Empty;
            }
            else
            {
                return value;
            }
        }

        private string SanitizeSearchQuery(string query)
        {
            return StripParenthetical(query).Trim();
        }

        // This is a very poor way of tokenizing words.
        private static readonly Regex NonWordRegex =
            new Regex(@"[a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string CreateSearchTerms(string query)
        {
            // TODO: Add ", FORMSOF (THESAURUS, \"{word}\") WEIGHT (0.25)" when
            // SQL Azure enables the THESAURUS in FTS.
            var words = from match in NonWordRegex.Matches(query)
                        let word = match.Value
                        where !String.IsNullOrWhiteSpace(word)
                        select $"ISABOUT (\"{word}\" WEIGHT (1), FORMSOF (INFLECTIONAL, \"{word}\") WEIGHT (0.75))";
            return String.Join(" AND ", words);
        }

        #endregion

        async Task<IEnumerable<SuggestionViewModel>> IDataService.GetSuggestionsAsync(string query)
        {
            var searchQuery = SanitizeSearchQuery(query);
            if (searchQuery.Length <= 0)
            {
                return Enumerable.Empty<SuggestionViewModel>();
            }
            var searchTerms = CreateSearchTerms(searchQuery);
            if (searchTerms.Length <= 0)
            {
                return Enumerable.Empty<SuggestionViewModel>();
            }
            const string queryText =
                @"
                SELECT
                    CNF_FoodName.FoodDescription AS [Name],
                    100.00 AS Serving,
                    Unit.UnitTypeCode AS ServingType,
                    Unit.UnitCode AS ServingCode,
                    CAST(CNF_NA_C.NutrientValue AS DECIMAL(7, 2)) AS CaloriesPerServing,
                    CAST(CNF_NA_P.NutrientValue AS DECIMAL(7, 2)) AS ProteinPerServing,
                    NULL AS Parenthetical,
                    Unit.Abbreviation AS UnitAbbreviation,
                    'm' as SuggestionTypeCode,
                    UnitType.SortOrder AS SortOrder1,
                    Unit.ConversionRatio AS SortOrder2,
                    Unit.[Name] AS SortOrder3
                FROM reciprocity.CNF_FoodName
                INNER JOIN reciprocity.CNF_NutrientAmount AS CNF_NA_C
                    ON CNF_NA_C.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientName AS CNF_NN_C
                    ON CNF_NA_C.NutrientId = CNF_NN_C.NutrientId
                    AND CNF_NN_C.NutrientSymbol = 'KCAL'
                INNER JOIN reciprocity.CNF_NutrientAmount AS CNF_NA_P
                    ON CNF_NA_P.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientName AS CNF_NN_P
                    ON CNF_NA_P.NutrientId = CNF_NN_P.NutrientId
                    AND CNF_NN_P.NutrientSymbol = 'PROT'
                INNER JOIN reciprocity.Unit
                    ON Unit.UnitTypeCode = 'm'
                    AND Unit.UnitCode = 'g'
                INNER JOIN reciprocity.UnitType
                    ON UnitType.UnitTypeCode = Unit.UnitTypeCode
                WHERE CNF_FoodName.FoodDescription = @searchQuery

                    UNION

                SELECT
                    CNF_FoodName.FoodDescription AS [Name],
                    CNF_Unit.Serving AS Serving,
                    CNF_Unit.ServingType AS ServingType,
                    CNF_Unit.ServingCode AS ServingCode,
                    CAST((CNF_NA_C.NutrientValue * CNF_ConversionFactor.ConversionFactorValue)
                            AS DECIMAL(7, 2)) AS CaloriesPerServing,
                    CAST((CNF_NA_P.NutrientValue * CNF_ConversionFactor.ConversionFactorValue)
                            AS DECIMAL(7, 2)) AS ProteinPerServing,
                    CNF_Unit.Parenthetical,
                    Unit.Abbreviation AS UnitAbbreviation,
                    'm' as SuggestionCode,
                    UnitType.SortOrder AS SortOrder1,
                    Unit.ConversionRatio AS SortOrder2,
                    Unit.[Name] AS SortOrder3
                FROM reciprocity.CNF_FoodName
                INNER JOIN reciprocity.CNF_NutrientAmount AS CNF_NA_C
                    ON CNF_NA_C.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientName AS CNF_NN_C
                    ON CNF_NA_C.NutrientId = CNF_NN_C.NutrientId
                    AND CNF_NN_C.NutrientSymbol = 'KCAL'
                INNER JOIN reciprocity.CNF_NutrientAmount AS CNF_NA_P
                    ON CNF_NA_P.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientName AS CNF_NN_P
                    ON CNF_NA_P.NutrientId = CNF_NN_P.NutrientId
                    AND CNF_NN_P.NutrientSymbol = 'PROT'
                INNER JOIN reciprocity.CNF_ConversionFactor
                    ON CNF_ConversionFactor.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_Unit
                    ON CNF_Unit.MeasureId = CNF_ConversionFactor.MeasureId
                INNER JOIN reciprocity.Unit
                    ON Unit.UnitTypeCode = CNF_Unit.ServingType
                    AND Unit.UnitCode = CNF_Unit.ServingCode
                INNER JOIN reciprocity.UnitType
                    ON UnitType.UnitTypeCode = Unit.UnitTypeCode
                WHERE CNF_FoodName.FoodDescription = @searchQuery
                ORDER BY SortOrder1, SortOrder2, SortOrder3;

                SELECT
                    CNF_FoodName.FoodDescription AS [Name],
                    100.00 AS Serving,
                    'm' AS ServingType,
                    'g' AS ServingCode,
                    CAST(CNF_NA_C.NutrientValue AS DECIMAL(7, 2)) AS CaloriesPerServing,
                    CAST(CNF_NA_P.NutrientValue AS DECIMAL(7, 2)) AS ProteinPerServing,
                    'i' as SuggestionTypeCode
                FROM reciprocity.CNF_FoodName
                INNER JOIN CONTAINSTABLE(reciprocity.CNF_FoodName, FoodDescription, @searchTerms, 25) AS SearchResult
                    ON SearchResult.[KEY] = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientAmount AS CNF_NA_C
                    ON CNF_NA_C.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientName AS CNF_NN_C
                    ON CNF_NA_C.NutrientId = CNF_NN_C.NutrientId
                    AND CNF_NN_C.NutrientSymbol = 'KCAL'
                INNER JOIN reciprocity.CNF_NutrientAmount AS CNF_NA_P
                    ON CNF_NA_P.FoodId = CNF_FoodName.FoodId
                INNER JOIN reciprocity.CNF_NutrientName AS CNF_NN_P
                    ON CNF_NA_P.NutrientId = CNF_NN_P.NutrientId
                    AND CNF_NN_P.NutrientSymbol = 'PROT'
                WHERE CNF_FoodName.FoodDescription <> @searchQuery
                ORDER BY SearchResult.[RANK] DESC, CNF_FoodName.FoodDescription;
                ";
            var queryParams = new
            {
                searchQuery,
                searchTerms
            };
            using (var connection = GetConnection())
            using (var results = await connection.QueryMultipleAsync(queryText, queryParams))
            {
                var measurements = results.Read<SuggestionModel>();
                var ingredients = results.Read<SuggestionModel>();
                return measurements.Concat(ingredients)
                    .Select(SuggestionViewModel.Create);
            }
        }
    }
}
