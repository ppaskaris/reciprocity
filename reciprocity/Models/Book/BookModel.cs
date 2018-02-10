using reciprocity.SecurityTheatre;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Models.Book
{
    public class BookModel
    {
        public Guid BookId { get; set; }
        public BearerToken Token { get; set; }
        public string Name { get; set; }
    }
}
