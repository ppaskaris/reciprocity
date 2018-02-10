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
    public class BookService : IBookService
    {
        private readonly IConnectionFactory _connectionFactory;

        public BookService(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        #region Helpers

        private SqlConnection GetConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        #endregion

        async Task<BookKeyModel> IBookService.CreateBookAsync(string name)
        {
            var book = new BookModel
            {
                BookId = Guid.NewGuid(),
                Token = BearerToken.CreateRandom(),
                Title = name
            };
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    INSERT INTO Book (BookId, Token, Title)
                    VALUES (@bookId, @token, @title);
                    ",
                    book);
            }
            return new BookKeyModel
            {
                BookId = book.BookId,
                Token = book.Token.ToString()
            };
        }

        async Task<BookModel> IBookService.GetBookAsync(Guid bookId)
        {
            using (var connection = GetConnection())
            {
                var book = await connection.QuerySingleOrDefaultAsync<BookModel>(
                    @"
                    SELECT BookId, Token, Title
                    FROM Book
                    WHERE BookId = @bookId;
                    ",
                    new { bookId });
                return book;
            }
        }

        async Task IBookService.RenameBookAsync(Guid bookId, string title)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    UPDATE Book
                    SET Title = @title
                    WHERE BookId = @bookId;
                    ",
                    new { bookId, title });
            }
        }

        async Task IBookService.DeleteBookAsync(Guid bookId)
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

        async Task<BookViewModel> IBookService.GetBookViewAsync(Guid bookId)
        {
            const string queryText =
                @"
                SELECT BookId, Title
                FROM Book
                WHERE BookId = @bookId;

                SELECT BookId, RecipeId, Title, Servings, AddedAt, LastModifiedAt
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
