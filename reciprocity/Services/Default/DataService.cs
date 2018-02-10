using Dapper;
using reciprocity.Data;
using reciprocity.Models.Book;
using reciprocity.Models.Recipe;
using reciprocity.SecurityTheatre;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
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
            var now = DateTime.Now;
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

                SELECT BookId, RecipeId, [Name], [Description], Servings, AddedAt, LastModifiedAt
                FROM BookRecipe
                WHERE BookId = @bookId;
                ";
            var queryParams = new { bookId };
            using (var connection = GetConnection())
            using (var query = await connection.QueryMultipleAsync(queryText, queryParams))
            {
                var book = await query.ReadFirstAsync<BookModel>();
                var recipes = await query.ReadAsync<RecipeModel>();
                return new BookViewModel
                {
                    Book = book,
                    Recipes = recipes
                };
            }
        }
    }
}
