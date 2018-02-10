using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Home
{
    public class CreateBookModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; }
    }
}
