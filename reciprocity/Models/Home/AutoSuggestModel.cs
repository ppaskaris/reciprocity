using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Home
{
    public class AutoSuggestModel
    {
        [Required]
        [MinLength(3)]
        public string Query { get; set; }
    }
}
