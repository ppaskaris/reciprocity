using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using reciprocity.Data;
using reciprocity.Models.Recipe;

namespace reciprocity.Services.Default
{
    public class RecipeService : IRecipeService
    {
        private IConnectionFactory _connectionFactory;

        public RecipeService(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        async Task<RecipeKeyModel> IRecipeService.CreateRecipeAsync(Guid bookId, string title, int servings)
        {
            var now = DateTime.Now;
            var recipe = new RecipeModel
            {
                BookId = bookId,
                RecipeId = Guid.NewGuid(),
                Title = title,
                Servings = servings,
                AddedAt = now,
                LastModifiedAt = now
            };
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    INSERT INTO BookRecipe (BookId, RecipeId, Title, Servings, AddedAt, LastModifiedat)
                    VALUES (@bookId, @recipeId, @title, @servings, @addedAt, @lastModifiedAt);
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

        async Task<RecipeModel> IRecipeService.GetRecipeAsync(Guid bookId, Guid recipeId)
        {
            using (var connection = GetConnection())
            {
                var recipe = await connection.QuerySingleOrDefaultAsync<RecipeModel>(
                    @"
                    SELECT BookId, RecipeId, Title, Servings, AddedAt, LastModifiedAt
                    FROM BookRecipe
                    WHERE BookId = @bookId
                        AND RecipeId = @recipeId;
                    ",
                    new { bookId, recipeId }
                );
                return recipe;
            }
        }

        private SqlConnection GetConnection()
        {
            return _connectionFactory.CreateConnection();
        }
    }
}
