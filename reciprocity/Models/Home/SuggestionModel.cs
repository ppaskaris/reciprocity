using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Home
{
    public class SuggestionModel
    {
        public string Name { get; set; }
        public decimal Serving { get; set; }
        public string ServingType { get; set; }
        public string ServingCode { get; set; }
        public string UnitAbbreviation { get; set; }
        public decimal CaloriesPerServing { get; set; }
        public string Parenthetical { get; set; }
        public string SuggestionTypeCode { get; set; }
    }
}
