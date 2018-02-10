using reciprocity.Models.Book;
using reciprocity.Models.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Services
{
    public interface IDataService
    {
        Task<BookKeyModel> CreateBookAsync(string name);
        Task<BookModel> GetBookAsync(Guid id);
        Task RenameBookAsync(Guid id, string name);
        Task DeleteBookAsync(Guid id);
        Task<RecipeKeyModel> CreateRecipeAsync(Guid bookId, AddRecipeModel recipe);
        Task<RecipeModel> GetRecipeAsync(Guid bookId, Guid recipeId);
        Task<BookViewModel> GetBookViewAsync(Guid bookId);
    }
}
