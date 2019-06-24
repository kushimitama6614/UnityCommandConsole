using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityCommandConsoleModule
{
    public string Name { get; set; }
    public string Description { get; set; }

    public delegate void FunctionDelegate(params string[] args);

    public Dictionary<string, FunctionDelegate> Commands = new Dictionary<string, FunctionDelegate>();

    public Exception LastError;

    public UnityCommandConsoleModule() {
        AddAllCommands();
    }

    public string GetLastError()
    {
        string errorMessage = LastError.Message;
        LastError = null;
        return errorMessage;
    }

    public bool RunCommand(string command, params string[] args)
    {
        try
        {
            Commands[command.ToLower()].Invoke(args);
            return true;
        }
        catch (Exception e)
        {
            LastError = e;
            return false;
        }
    }

    private void AddAllCommands()
    {
        // Add class methods to Commands dictionary here
        // Commands.Add("command", functionName);
    }

    // Define module methods here,
    // methods should have a signature that matches the FunctionDelegate type e.g: 
    // private void functionName(params string[] args)
}
