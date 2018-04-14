using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class IngredientModel
    {
        public Guid BookId { get; set; }
        public Guid RecipeId { get; set; }
        public int IngredientNo { get; set; }

        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public string QuantityType { get; set; }
        public string QuantityUnit { get; set; }
        public decimal Serving { get; set; }
        public string ServingType{ get; set; }
        public string ServingUnit { get; set; }
        public decimal CaloriesPerServing { get; set; }
        public decimal ProteinPerServing { get; set; }
    }
}
