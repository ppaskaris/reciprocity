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
        public const int MeasurementMax = 1000;
        public const string MeasurementPattern = @"^(0|\d{0,5}(\.\d{0,2})?)$";
        public const string MeasurementErrorMessage = @"The {0} field can have at most 5 integer digits and 2 fractional digits.";

        public const string UnitKeyPattern = @"^([a-z]),([a-z]{1,3})$";
    }
}
