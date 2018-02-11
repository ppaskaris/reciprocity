using reciprocity.Models.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Book
{
    public class BookViewModel
    {
        public BookModel Book { get; set; }
        public IEnumerable<RecipeListItemViewModel> Recipes {get;set;}
    }
}
