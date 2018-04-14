using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Home
{
    public class SuggestionViewModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public decimal Serving { get; set; }
        public string ServingUnit { get; set; }
        public decimal CaloriesPerServing { get; set; }
        public decimal ProteinPerServing { get; set; }
        public bool IsTerminal { get; set; }
        public string Category { get; set; }

        public static SuggestionViewModel Create(SuggestionModel suggestion)
        {
            bool isTerminal;
            string name, value, category;
            if (suggestion.Parenthetical != null)
            {
                value = $"{suggestion.Name} ({suggestion.Parenthetical})";
                if (suggestion.ServingType != Constants.QuantityUnitTypeCode)
                {
                    name = $"{suggestion.Name} ({suggestion.Serving} {suggestion.UnitAbbreviation}, {suggestion.Parenthetical})";
                }
                else
                {
                    name = value;
                }
            }
            else if (suggestion.UnitAbbreviation != null)
            {
                name = $"{suggestion.Name} ({suggestion.Serving} {suggestion.UnitAbbreviation})";
                value = suggestion.Name;
            }
            else
            {
                name = value = suggestion.Name;
            }
            switch (suggestion.SuggestionTypeCode)
            {
                case Constants.MeasurementSuggestionTypeCode:
                    isTerminal = true;
                    category = "Weights & Measures";
                    break;
                case Constants.IngredientSuggestionTypeCode:
                    category = "Ingredients";
                    isTerminal = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(suggestion.SuggestionTypeCode));
            }
            return new SuggestionViewModel
            {
                Name = name,
                Value = value,
                CaloriesPerServing = suggestion.CaloriesPerServing,
                ProteinPerServing = suggestion.ProteinPerServing,
                Serving = suggestion.Serving,
                ServingUnit = $"{suggestion.ServingType},{suggestion.ServingCode}",
                IsTerminal = isTerminal,
                Category = category
            };
        }
    }
}
