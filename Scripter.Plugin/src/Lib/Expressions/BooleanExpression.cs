﻿namespace ScripterLang
{
    public class BooleanExpression : Expression
    {
        public BooleanExpression(bool value)
        {
            Value = value;
        }

        public bool Value { get; }

        public override Value Evaluate(RuntimeLexicalContext lexicalContext)
        {
            return ScripterLang.Value.CreateBoolean(Value);
        }
    }
}
