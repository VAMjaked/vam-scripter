﻿using System.Collections.Generic;

namespace SplitAndMerge
{
    class AddFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            Variable currentValue = Utils.GetSafeVariable(args, 0);
            Variable item = Utils.GetSafeVariable(args, 1);
            int index = Utils.GetSafeInt(args, 2, -1);

            currentValue.AddVariable(item, index);
            if (!currentValue.ParsingToken.Contains(Constants.START_ARRAY.ToString()))
            {
                InterpreterInstance.AddGlobalOrLocalVariable(currentValue.ParsingToken,
                    new GetVarFunction(currentValue), script);
            }

            return currentValue;
        }

    }
}
