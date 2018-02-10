using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class RecipeModel
    {
        public Guid BookId { get; set; }
        public Guid RecipeId { get; set; }

        public string Title { get; set; }
        public int Servings { get; set; }
        public TimeSpan ReadyIn { get; set; }

        public DateTime AddedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }
}
