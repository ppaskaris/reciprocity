using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Recipe
{
    public interface IRecipeStatsViewModel
    {
        int Servings { get; }
        int CaloriesPerServing { get; }
        decimal ProteinPerServing { get; }
        TimeSpan ReadyIn { get; }
        DateTime AddedAt { get; }
        DateTime LastModifiedAt { get; }
    }
}
