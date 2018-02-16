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
        public bool IsTerminal { get; set; }

        public static SuggestionViewModel Create(SuggestionModel suggestion)
        {
            string name, value;
            if (suggestion.Parenthetical != null)
            {
                if (suggestion.ServingType == "v")
                {
                    name = $"{suggestion.Name} ({suggestion.Serving} {suggestion.UnitAbbreviation}, {suggestion.Parenthetical})";
                    value = $"{suggestion.Name} ({suggestion.Parenthetical})";
                }
                else
                {
                    name = value = $"{suggestion.Name} ({suggestion.Parenthetical})";
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
            return new SuggestionViewModel
            {
                Name = name,
                Value = value,
                CaloriesPerServing = suggestion.CaloriesPerServing,
                Serving = suggestion.Serving,
                ServingUnit = $"{suggestion.ServingType},{suggestion.ServingCode}",
                IsTerminal = suggestion.IsTerminal
            };
        }
    }
}
