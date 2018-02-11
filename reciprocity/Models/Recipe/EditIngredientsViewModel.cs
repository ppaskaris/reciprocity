using reciprocity.Models.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class EditIngredientsViewModel
    {
        public BookModel Book { get; set; }
        public RecipeModel Recipe { get; set; }
        public List<EditIngredientViewModel> Ingredients { get; set; }
    }
}
