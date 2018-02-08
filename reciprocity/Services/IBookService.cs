using reciprocity.Models.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Services
{
    public interface IBookService
    {
        Task<BookModel> CreateNewBookAsync(string name);
        Task<BookModel> GetBookAsync(Guid id);
        Task RenameBookAsync(Guid id, string title);
        Task DeleteBookAsync(Guid id);
    }
}
