using Dapper;
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

        async Task<RecipeKeyModel> IDataService.CreateRecipeAsync(Guid bookId, AddRecipeModel fragment)
        {
            var now = DateTime.UtcNow;
            var recipe = new RecipeModel
            {
                BookId = bookId,
                RecipeId = Guid.NewGuid(),
                Name = fragment.Name,
                Description = fragment.Description,
                Servings = fragment.Servings,
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

        async Task IDataService.UpdateRecipeAsync(Guid bookId, Guid recipeId, EditRecipeModel fragment)
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
                        name = fragment.Name,
                        description = fragment.Description,
                        servings = fragment.Servings,
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
                        CaloriesPerServing
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
            new SqlMetaData("Name", SqlDbType.NVarChar, 100),
            new SqlMetaData("Quantity", SqlDbType.Decimal, 7, 2),
            new SqlMetaData("QuantityType", SqlDbType.Char, 1),
            new SqlMetaData("QuantityUnit", SqlDbType.VarChar, 3),
            new SqlMetaData("Serving", SqlDbType.Decimal, 7, 2),
            new SqlMetaData("ServingType", SqlDbType.Char, 1),
            new SqlMetaData("ServingUnit", SqlDbType.VarChar, 3),
            new SqlMetaData("CaloriesPerServing", SqlDbType.Decimal, 7, 2),
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
                                CaloriesPerServing = source.CaloriesPerServing
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
                                CaloriesPerServing
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
                                source.CaloriesPerServing
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

        async Task<(IEnumerable<IngredientViewModel>, int)> IDataService.GetRecipeViewComponentsAsync(Guid bookId, Guid recipeId)
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

                SELECT CaloriesPerServing
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
                var caloriesPerServing = await query.ReadFirstOrDefaultAsync<int>();
                return (ingredients, caloriesPerServing);
            }
        }

        #region GetSuggestionsAsync Helpers

        // This is a very poor way of tokenizing words.
        private static readonly Regex NonWordRegex =
            new Regex(@"[a-z]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string CreateSearchTerms(string query)
        {
            var words = from match in NonWordRegex.Matches(query)
                        where !String.IsNullOrWhiteSpace(match.Value)
                        select $"FORMSOF (INFLECTIONAL, {match.Value})";
            return String.Join(" AND ", words);
        }

        #endregion

        async Task<IEnumerable<SuggestionViewModel>> IDataService.GetSuggestionsAsync(string query)
        {
            var terms = CreateSearchTerms(query);
            using (var connection = GetConnection())
            {
                var suggestions = await connection.QueryAsync<SuggestionModel>(
                    @"
                    SELECT
	                    CNF_FoodName.FoodDescription AS [Name],
                        CNF_Unit.Serving,
                        CNF_Unit.ServingType,
                        CNF_Unit.ServingCode,
	                    CAST((CNF_NutrientAmount.NutrientValue * CNF_ConversionFactor.ConversionFactorValue)
                             AS DECIMAL(7, 2)) AS CaloriesPerServing,
                        CNF_Unit.Parenthetical,
                        Unit.Abbreviation AS UnitAbbreviation,
                        CAST(1 AS BIT) AS IsTerminal
                    FROM reciprocity.CNF_FoodName
                    INNER JOIN reciprocity.CNF_NutrientAmount
	                    ON CNF_NutrientAmount.FoodId = CNF_FoodName.FoodId
                    INNER JOIN reciprocity.CNF_NutrientName
	                    ON CNF_NutrientAmount.NutrientId = CNF_NutrientName.NutrientId
                    INNER JOIN reciprocity.CNF_ConversionFactor
                        ON CNF_ConversionFactor.FoodId = CNF_FoodName.FoodId
                    INNER JOIN reciprocity.CNF_Unit
                        ON CNF_Unit.MeasureId = CNF_ConversionFactor.MeasureId
                    INNER JOIN reciprocity.Unit
                        ON Unit.UnitTypeCode = CNF_Unit.ServingType
                        AND Unit.UnitCode = CNF_Unit.ServingCode
                    WHERE CNF_FoodName.FoodDescription = @query
                        AND CNF_NutrientName.NutrientSymbol = 'KCAL'

                    UNION ALL

                    SELECT
	                    CNF_FoodName.FoodDescription AS [Name],
                        100.00 AS Serving,
                        'm' AS ServingType,
                        'g' AS ServingCode,
	                    CAST(CNF_NutrientAmount.NutrientValue AS DECIMAL(7, 2)) AS CaloriesPerServing,
                        NULL AS Paranthetical,
                        NULL AS UnitAbbreviation,
                        CAST(0 AS BIT) AS IsTerminal
                    FROM reciprocity.CNF_FoodName
                    INNER JOIN CONTAINSTABLE(reciprocity.CNF_FoodName, FoodDescription, @terms, 25) AS SearchResult
	                    ON SearchResult.[KEY] = CNF_FoodName.FoodId
                    INNER JOIN reciprocity.CNF_NutrientAmount
	                    ON CNF_NutrientAmount.FoodId = CNF_FoodName.FoodId
                    INNER JOIN reciprocity.CNF_NutrientName
	                    ON CNF_NutrientAmount.NutrientId = CNF_NutrientName.NutrientId
                    WHERE CNF_NutrientName.NutrientSymbol = 'KCAL';
                    ",
                    new
                    {
                        query,
                        terms
                    });
                return suggestions.Select(SuggestionViewModel.Create);
            }
        }
    }
}
