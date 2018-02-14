using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Home
{
    public class SuggestionViewModel
    {
        public string Name { get; set; }
        public decimal Serving => 100.00m;
        public string ServingUnit => "m,g";
        public decimal CaloriesPerServing { get; set; }
    }
}
