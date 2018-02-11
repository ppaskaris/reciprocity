using reciprocity.Models.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class RecipeViewModel
    {
        public BookModel Book { get; set; }
        public RecipeModel Recipe { get; set; }
        public IEnumerable<IngredientViewModel> Ingredients { get; set; }
    }
}
