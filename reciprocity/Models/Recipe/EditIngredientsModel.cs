using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class EditIngredientsModel
    {
        [Required]
        [MinLength(1)]
        public List<EditIngredientModel> Ingredients { get; set; }

        public bool AddNew { get; set; }

        [Required]
        public SaveActionType? SaveAction { get; set; }
    }
}
