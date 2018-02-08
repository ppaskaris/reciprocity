using reciprocity.Models.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Services
{
    public interface IRecipeService
    {
        Task<RecipeKeyModel> CreateRecipeAsync(Guid bookId, string title, int servings);
        Task<RecipeModel> GetRecipeAsync(Guid bookId, Guid recipeId);
    }
}
