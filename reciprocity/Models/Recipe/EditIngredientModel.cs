using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public class EditIngredientModel : IValidatableObject
    {
        [Required]
        [Range(0, 100)]
        [Display(Name = "#")]
        public int? IngredientNo { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(Constants.MeasurementMin, Constants.MeasurementMax)]
        public decimal? Quantity { get; set; }

        [Required]
        [RegularExpression(Constants.UnitKeyPattern)]
        [Display(Name = "Unit")]
        public string QuantityUnit { get; set; }

        [Required]
        [Range(Constants.MeasurementMin, Constants.MeasurementMax)]
        public decimal? Serving { get; set; }

        [Required]
        [RegularExpression(Constants.UnitKeyPattern)]
        [Display(Name = "Unit")]
        public string ServingUnit { get; set; }

        [Required]
        [Range(Constants.MeasurementMin, Constants.MeasurementMax)]
        [Display(Name = "Calories per serving")]
        public decimal? CaloriesPerServing { get; set; }

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            if (QuantityUnit[0] != ServingUnit[0])
            {
                yield return new ValidationResult(
                    "The Unit fields must agree on the type of unit (mass or volume).",
                    new[]
                    {
                        "QuantityUnit",
                        "ServingUnit"
                    });
            }
        }
    }
}
