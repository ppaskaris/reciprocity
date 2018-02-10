using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class EditRecipeModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(1, 100)]
        public int Servings { get; set; }
    }
}
