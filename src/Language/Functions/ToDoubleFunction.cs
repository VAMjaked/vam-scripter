﻿using System.Collections.Generic;

namespace SplitAndMerge
{
    class ToDoubleFunction : ParserFunction, INumericFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name, true);
            Variable arg = args[0];

            double result = Utils.ConvertToDouble(arg.AsString());
            return new Variable(result);
        }
    }
}
