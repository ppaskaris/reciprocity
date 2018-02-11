using reciprocity.Models.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Book
{
    public class RecipeListItemViewModel : IRecipeStatsViewModel
    {
        public Guid RecipeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int Servings { get; set; }
        public int CaloriesPerServing { get; set; }
        public TimeSpan ReadyIn { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }
}
