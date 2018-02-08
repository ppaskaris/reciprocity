using reciprocity.Models.Book;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class RecipeKeyModel : BookKeyModel
    {
        [Required]
        public Guid? RecipeId { get; set; }
    }
}
