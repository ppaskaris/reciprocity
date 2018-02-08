using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Data
{
    public interface IConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}
