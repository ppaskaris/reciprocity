﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Book
{
    public class BookKeyModel
    {
        [Required]
        public Guid? BookId { get; set; }
        [Required]
        public string Token { get; set; }
    }
}
