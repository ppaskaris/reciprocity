using Microsoft.AspNetCore.Mvc.Rendering;
using reciprocity.Models.Book;
using reciprocity.Models.Home;
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
        Task<BookModel> GetBookAsync(Guid bookId);
        Task RenameBookAsync(Guid bookId, string name);
        Task DeleteBookAsync(Guid bookId);
        Task<RecipeKeyModel> CreateRecipeAsync(Guid bookId, AddRecipeModel model);
        Task<RecipeModel> GetRecipeAsync(Guid bookId, Guid recipeId);
        Task<BookViewModel> GetBookViewAsync(Guid bookId);
        Task UpdateRecipeAsync(Guid bookId, Guid recipeId, EditRecipeModel model);
        Task DeleteRecipeAsync(Guid bookId, Guid recipeId);
        Task<IEnumerable<SelectListItem>> GetUnitsAsync();
        Task<IEnumerable<IngredientModel>> GetIngredientsAsync(Guid bookId, Guid recipeId);
        Task SaveIngredientsAsync(Guid bookId, Guid recipeId, IEnumerable<EditIngredientModel> ingredients);
        Task<(IEnumerable<IngredientViewModel>, int, decimal)> GetRecipeViewComponentsAsync(Guid bookId, Guid recipeId);
        Task<IEnumerable<SuggestionViewModel>> GetSuggestionsAsync(string query);
    }
}
