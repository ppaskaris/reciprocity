using Dapper;
using reciprocity.Data;
using reciprocity.Models.Book;
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

        async Task<BookModel> IBookService.GetBookAsync(Guid id)
        {
            using (var connection = GetConnection())
            {
                var book = await connection.QuerySingleOrDefaultAsync<BookModel>(
                    @"
                    SELECT BookId, Token, Title
                    FROM Book
                    WHERE BookId = @id;
                    ",
                    new { id });
                return book;
            }
        }

        async Task IBookService.RenameBookAsync(Guid id, string title)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    UPDATE Book
                    SET Title = @title
                    WHERE BookId = @id;
                    ",
                    new { id, title });
            }
        }

        async Task IBookService.DeleteBookAsync(Guid id)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    DELETE FROM Book
                    WHERE BookId = @id;
                    ",
                    new { id });
            }
        }

        private SqlConnection GetConnection()
        {
            return _connectionFactory.CreateConnection();
        }
    }
}
