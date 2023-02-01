﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SplitAndMerge
{
  public class ParserFunction
  {
    public ParserFunction()
    {
      m_impl = this;
    }

    // A "virtual" Constructor
    internal ParserFunction(string data, ref int from, string item, char ch, ref string action)
    {
      if (item.Length == 0 && (ch == Constants.START_ARG || from >= data.Length)) {
        // There is no function, just an expression in parentheses
        m_impl = s_idFunction;
        return;
      }

      m_impl = GetArrayFunction(item, ref from, action);
      if (m_impl != null) {
        return;
      }

      m_impl = GetFunctionOrAction(item, ref action);

      if (m_impl == s_strOrNumFunction && string.IsNullOrWhiteSpace (item))  {
        string problem = (!string.IsNullOrWhiteSpace (action) ? action : ch.ToString());
        string restData = ch.ToString() +
          data.Substring(from, Math.Min(data.Length - from -1, Constants.MAX_ERROR_CHARS));
        throw new ArgumentException("Couldn't parse [" + problem + "] in " + restData + "...");
      }         
    }

    public static ParserFunction GetArrayFunction(string name, ref int from, string action)
    {
      if (!string.IsNullOrWhiteSpace(action)) {
        return null;
      }
        
      int arrayStart = name.IndexOf(Constants.START_ARRAY);
      if (arrayStart <= 0) {
        return null;
      }

      int origLength = name.Length;
      int arrayIndex = Utils.ExtractArrayElement(ref name);
      if (arrayIndex < 0) {
        return null;
      }
      ParserFunction pf = ParserFunction.GetFunction(name);
      if (pf == null) {
        return null;
      }

      from -= (origLength - arrayStart - 1);
      return pf;
    }

    public static ParserFunction GetFunctionOrAction(string item, ref string action)
    {
      ActionFunction actionFunction = GetAction(action);

      // If passed action exists and is registered we are done.
      if (actionFunction != null) {
        ActionFunction theAction = actionFunction.NewInstance() as ActionFunction;
        theAction.Name = item;
        theAction.Action = action;

        action = null;
        return theAction;
      }

      // Otherwise look for local and global functions.
      ParserFunction pf =  GetFunction(item);

      if (pf != null) {
        return pf;
      }

      // Function not found, will try to parse this as a string in quotes or a number.
      s_strOrNumFunction.Item = item;
      return s_strOrNumFunction;
    }

    public static ParserFunction GetFunction(string item)
    {
      ParserFunction impl;
      // First search among local variables.

      if (s_locals.Count > 0) {
        Dictionary<string, ParserFunction> local = s_locals.Peek().Variables;
        if (local.TryGetValue(item, out impl))
        {
          // Local function exists (a local variable)
          return impl;
        }
      }
      if (s_functions.TryGetValue(item, out impl))
      {
        // Global function exists and is registered (e.g. pi, exp, or a variable)
        return impl.NewInstance();
      }

      return null;
    }

    public static ActionFunction GetAction(string action)
    {
      if (string.IsNullOrWhiteSpace (action)) {
        return null;
      }

      ActionFunction impl;
      if (s_actions.TryGetValue(action, out impl))
      {
        // Action exists and is registered (e.g. =, +=, --, etc.)
        return impl;
      }

      return null;
    }

    public static bool FunctionExists(string item)
    {
      bool exists = false;
      // First check if the local function stack has this variable defined.
      if (s_locals.Count > 0) {
        Dictionary<string, ParserFunction> local = s_locals.Peek().Variables;
        exists = local.ContainsKey(item);
      }

      // If it is not defined locally, then check globally:
      return exists || s_functions.ContainsKey(item);
    }

    public static void AddGlobalOrLocalVariable(string name, ParserFunction function)
    {
      function.Name = name;
      if (s_locals.Count > 0) {
        AddLocalVariable(function);
      } else {
        AddGlobal(name, function);
      }
    }

    public static void AddGlobal(string name, ParserFunction function)
    {
      s_functions[name] = function;
      function.Name     = name;
    }

    public static void AddAction(string name, ActionFunction action)
    {
      s_actions[name] = action;
    }

    public static void AddLocalVariables(StackLevel locals)
    {
      s_locals.Push(locals);
    }

    public static void AddStackLevel(string name)
    {
      s_locals.Push(new StackLevel(name));
    }

    public static void AddLocalVariable(ParserFunction local)
    {
      StackLevel locals = null;
      if (s_locals.Count == 0) {
        locals = new StackLevel();
        s_locals.Push(locals);
      } else {
        locals = s_locals.Peek();
      }

      locals.Variables[local.Name] = local;
    }

    public static void PopLocalVariables()
    {
      s_locals.Pop();
    }

    public static int GetCurrentStackLevel()
    {
      return s_locals.Count;
    }

    public static void InvalidateStacksAfterLevel(int level)
    {
      while (s_locals.Count > level) {
        s_locals.Pop();
      }
    }

    public static void PopLocalVariable(string name)
    {
      if (s_locals.Count == 0) {
        return;
      }
      Dictionary<string, ParserFunction> locals = s_locals.Peek().Variables;
      locals.Remove(name);
    }

    public Variable GetValue(string data, ref int from)
    {
      return m_impl.Evaluate(data, ref from);
    }

    protected virtual Variable Evaluate(string data, ref int from)
    {
      // The real implementation will be in the derived classes.
      return new Variable();
    }

    public virtual ParserFunction NewInstance() {
      return this;
    }

    protected string m_name;
    public string Name { get { return m_name; } set { m_name = value; } }

    private ParserFunction m_impl;
    // Global functions:
    private static Dictionary<string, ParserFunction> s_functions = new Dictionary<string, ParserFunction>();

    // Global actions - function:
    private static Dictionary<string, ActionFunction> s_actions = new Dictionary<string, ActionFunction>();

    public class StackLevel {
      public StackLevel(string name = null) {
        Name = name;
        Variables = new Dictionary<string, ParserFunction>();
      }
      public string Name { get; set; }
      public Dictionary<string, ParserFunction> Variables { get; set; }
    }

    // Local variables:
    // Stack of the functions being executed:
    private static Stack<StackLevel> s_locals = new Stack<StackLevel>();
    public  static Stack<StackLevel> ExecutionStack { get { return s_locals; } }

    private static StringOrNumberFunction s_strOrNumFunction =
      new StringOrNumberFunction();
    private static IdentityFunction s_idFunction =
      new IdentityFunction();
  }

  public abstract class ActionFunction : ParserFunction
  {
    protected string  m_action;
    public string Action { set { m_action = value; } }
  }

}
