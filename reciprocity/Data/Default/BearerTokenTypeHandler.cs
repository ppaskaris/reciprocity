using Dapper;
using reciprocity.SecurityTheatre;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace reciprocity.Data.Default
{
    public class BearerTokenTypeHandler : SqlMapper.TypeHandler<BearerToken>
    {
        public override BearerToken Parse(object value)
        {
            return BearerToken.FromBytes((byte[])value);
        }

        public override void SetValue(IDbDataParameter parameter, BearerToken value)
        {
            parameter.Value = value.ToBytes();
        }
    }
}
