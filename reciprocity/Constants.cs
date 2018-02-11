using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace reciprocity
{
    public static class Constants
    {
        public const int MeasurementMin = 0;
        public const int MeasurementMax = 10000;

        public const string UnitKeyPattern = @"^([a-z]),([a-z]{1,3})$";
    }
}
