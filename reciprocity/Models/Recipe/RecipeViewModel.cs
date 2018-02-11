using reciprocity.Models.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class RecipeViewModel : IRecipeStatsViewModel
    {
        public BookModel Book { get; set; }
        public RecipeModel Recipe { get; set; }
        public IEnumerable<IngredientViewModel> Ingredients { get; set; }
        public int CaloriesPerServing { get; set; }

        int IRecipeStatsViewModel.Servings => Recipe.Servings;
        TimeSpan IRecipeStatsViewModel.ReadyIn => Recipe.ReadyIn;
        DateTime IRecipeStatsViewModel.AddedAt => Recipe.AddedAt;
        DateTime IRecipeStatsViewModel.LastModifiedAt => Recipe.LastModifiedAt;
    }
}
