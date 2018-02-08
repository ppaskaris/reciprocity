using Dapper;
using reciprocity.Data;
using reciprocity.Models.Book;
using reciprocity.SecurityTheatre;
using System;
using System.Collections.Generic;
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

        async Task<BookModel> IBookService.CreateNewBookAsync(string name)
        {
            var book = new BookModel
            {
                Id = Guid.NewGuid(),
                Token = BearerToken.CreateRandom(),
                Title = name
            };
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    INSERT INTO book (id, token, title)
                    VALUES (@id, @token, @title);
                    ",
                    book);
            }
            return book;
        }

        async Task<BookModel> IBookService.GetBookAsync(Guid id)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                var book = await connection.QuerySingleOrDefaultAsync<BookModel>(
                    @"
                    SELECT id, token, title
                    FROM book
                    WHERE id = @id;
                    ",
                    new { id });
                return book;
            }
        }

        async Task IBookService.RenameBookAsync(Guid id, string title)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    UPDATE book
                    SET title = @title
                    WHERE id = @id;
                    ",
                    new { id, title });
            }
        }

        async Task IBookService.DeleteBookAsync(Guid id)
        {
            using (var connection = _connectionFactory.CreateConnection())
            {
                await connection.ExecuteAsync(
                    @"
                    DELETE FROM book
                    WHERE id = @id;
                    ",
                    new { id });
            }
        }
    }
}
