using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Home
{
    public class AutoSuggestViewModel : AutoSuggestModel
    {
        public IEnumerable<SuggestionViewModel> Suggestions { get; set; }
    }
}
