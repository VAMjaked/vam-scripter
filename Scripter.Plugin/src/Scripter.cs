using System;
using System.Collections.Generic;
using ScripterLang;
using UnityEngine;
using UnityEngine.UI;
using Vam;

public class Scripter : MVRScript
{
    private GlobalLexicalContext _globalLexicalContext;
    private RuntimeDomain _domain;
    private Expression _expression;

    private readonly JSONStorableString _scriptJSON;
    private readonly JSONStorableAction _executeScriptJSON;
    private readonly JSONStorableString _consoleJSON;
    private readonly List<string> _history = new List<string>();

    public Scripter()
    {
        _scriptJSON = new JSONStorableString("Script", "");
        _executeScriptJSON = new JSONStorableAction("Execute", ExecuteScript);
        _consoleJSON = new JSONStorableString("Console", "");
    }

    private void ExecuteScript()
    {
        ProcessScript();
    }

    public override void Init()
    {
        _globalLexicalContext = new GlobalLexicalContext();
        VamFunctions.Register(_globalLexicalContext);;

        _scriptJSON.valNoCallback = @"
// Welcome to Scripter!
var alpha = getFloatParamValue(""Cube"", ""CubeMat"", ""Alpha Adjust"", 0.5);
if(alpha == 0) {
    logMessage(""The cube is fully transparent"");
} else {
    logMessage(""The cube alpha is: "" + alpha);
}
".Trim();
        _history.Add(_scriptJSON.val);

        _scriptJSON.setCallbackFunction = val =>
        {
            _history.Add(val);
            if (_history.Count > 100) _history.RemoveAt(0);
            Parse(_scriptJSON.val);
        };

        RegisterString(_scriptJSON);
        RegisterAction(_executeScriptJSON);

        CreateButton("Execute").button.onClick.AddListener(_executeScriptJSON.actionCallback.Invoke);
        CreateTextInput(_scriptJSON);

        // TODO: Toolbar
        CreateButton("Undo").button.onClick.AddListener(Undo);

        CreateTextField(_consoleJSON);

        Parse(_scriptJSON.val);
    }

    private void Parse(string val)
    {
        try
        {
            _expression = Parser.Parse(val, _globalLexicalContext);
            _domain = new RuntimeDomain(_globalLexicalContext);
            _consoleJSON.val = "Code parsed successfully";
        }
        catch (Exception exc)
        {
            _expression = null;
            _domain = null;
            _consoleJSON.val = exc.ToString();
        }
    }

    private void Undo()
    {
        // TODO: Improve this (undo should not delete history, just go back)
        if (_history.Count == 0) return;
        _scriptJSON.valNoCallback = _history[_history.Count - 1];
        _history.RemoveAt(_history.Count - 1);
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null) return;
        leftUIContent.anchorMax = new Vector2(1, 1);
    }

    private UIDynamicTextField CreateTextInput(JSONStorableString jss)
    {
        var textfield = CreateTextField(jss);
        textfield.height = 700;
        jss.dynamicText = textfield;
        textfield.backgroundColor = Color.white;
        var text = textfield.GetComponentInChildren<Text>(true);
        var input = text.gameObject.AddComponent<InputField>();
        input.lineType = InputField.LineType.MultiLineNewline;
        input.textComponent = textfield.UItext;
        jss.inputField = input;
        return textfield;
    }

    private void ProcessScript()
    {
        try
        {
            if (_expression == null)
                throw new ScripterPluginException("The code was not parsed. Please check the console for errors.");
            _expression.Evaluate(_domain);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Scripter: There was an error executing the script.\n{exc.Message}");
            _consoleJSON.val = exc.ToString();
        }
    }
}
