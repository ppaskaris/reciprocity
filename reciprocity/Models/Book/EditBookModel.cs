using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Book
{
    public class EditBookModel
    {
        [Required, StringLength(100)]
        public string Title { get; set; }
    }
}
