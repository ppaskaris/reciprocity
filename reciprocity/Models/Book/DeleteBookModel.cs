﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Book
{
    public class DeleteBookModel
    {
        [Required, Range(typeof(bool), "true", "true")]
        public bool? Confirm { get; set; }
    }
}
