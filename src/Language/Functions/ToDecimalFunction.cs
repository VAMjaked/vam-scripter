﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace SplitAndMerge
{
    class ToDecimalFunction : ParserFunction, INumericFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name, true);
            Variable arg = args[0];

            string result = Decimal.Parse(arg.AsString(), NumberStyles.Any).ToString();
            return new Variable(result);
        }
    }
}
