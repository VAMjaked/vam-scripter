﻿using System.Text.RegularExpressions;

namespace ScripterLang
{
    public class PropertyAccessor : VariableAccessor
    {
        private readonly Expression _left;
        private readonly string _property;
        private ObjectReference _object;

        public PropertyAccessor(Expression left, string property)
        {
            _left = left;
            _property = string.Intern(property);
        }

        public override void Bind()
        {
            _left.Bind();
        }

        public override Value Evaluate()
        {
            var value = _left.Evaluate();
            if (value.IsString)
            {
                switch (_property)
                {
                    case "length":
                        return value.AsString.Length;
                    case "startsWith":
                        return new FunctionReference(((context, args) => value.AsString.StartsWith(args[0].AsString)));
                    case "endsWith":
                        return new FunctionReference(((context, args) => value.AsString.EndsWith(args[0].AsString)));
                    case "contains":
                        return new FunctionReference(((context, args) => value.AsString.Contains(args[0].AsString)));
                    default:
                        throw new ScripterRuntimeException("There is no property or function named " + _property + " on type string.");
                }
            }
            return value.AsObject.GetProperty(_property);
        }

        public override void SetVariableValue(Value setValue)
        {
            var value = _left.Evaluate();
            value.AsObject.SetProperty(_property, setValue);
        }

        public override Value GetAndHold()
        {
            var value = _left.Evaluate();
            _object = value.AsObject;
            return _object.GetProperty(_property);
        }

        public override void Release()
        {
            _object = null;
        }

        public override void SetAndRelease(Value value)
        {
            _object.SetProperty(_property, value);
            _object = null;
        }

        public override string ToString()
        {
            return $"{_left}.{_property}";
        }
    }
}
