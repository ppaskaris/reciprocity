using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class EditIngredientViewModel : EditIngredientModel
    {
        public bool AutoFocus { get; set; }
        public IEnumerable<SelectListItem> Units { get; set; }
    }
}
