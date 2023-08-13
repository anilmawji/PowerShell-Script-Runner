﻿using ITPortal.Lib.Services.Automation.Output;
using ITPortal.Lib.Services.Automation.Script.Parameter;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

namespace ITPortal.Lib.Services.Automation.Script;

public class PowerShellScript : AutomationScript
{
    private readonly InitialSessionState _initialPowerShellState;

    public PowerShellScript(IScriptOutputStreamService outputStreamService) : base(outputStreamService)
    {
        _initialPowerShellState = NewInitialSessionState();
    }

    public PowerShellScript(IScriptOutputStreamService outputStreamService, string filePath) : base(outputStreamService, filePath)
    {
        _initialPowerShellState = NewInitialSessionState();
    }

    private static InitialSessionState NewInitialSessionState()
    {
        // CreateDefault() only loads the commands necessary to host PowerShell, CreateDefault2() loads all available commands
        InitialSessionState initialPowerShellState = InitialSessionState.CreateDefault();
        // Limit script execution to one thread
        initialPowerShellState.ApartmentState = ApartmentState.STA;
        // Set execution policy of the PS session
        initialPowerShellState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

        return initialPowerShellState;
    }

    public override bool LoadFromFile(string filePath)
    {
        if (!base.LoadFromFile(filePath))
        {
            return false;
        }
        return LoadParameters();
    }

    public override bool Refresh()
    {
        if (!base.Refresh())
        {
            return false;
        }
        return LoadParameters();
    }

    public bool LoadParameters()
    {
        ScriptBlockAst scriptAst = Parser.ParseInput(GetContentAsString(), out _, out ParseError[] errors);

        if (errors.Length != 0)
        {
            LoadState = ScriptLoadState.Failed;
            return false;
        }

        if (scriptAst.ParamBlock != null)
        {
            Parameters = new PowershellParameterList(scriptAst.ParamBlock.Parameters);
        }
        else
        {
            Parameters = new PowershellParameterList();
        }

        return true;
    }

    public override async Task InvokeAsync(string cancellationMessage, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            OutputStreamService?.AddOutput(ScriptStreamType.Warning, cancellationMessage);
            return;
        }

        if (!IsLoaded())
        {
            throw new InvalidPowerShellStateException("Attempt to invoke a script that was not loaded");
        }

        try
        {
            // "using" relies on compiler to dispose of shell when method is popped from call stack
            using PowerShell shell = PowerShell.Create(_initialPowerShellState);
            shell.AddScript(GetContentAsString());
            ((PowershellParameterList?)Parameters)?.Register(shell);

            PSDataCollection<PSObject> outputCollection = new();

            if (OutputStreamService != null)
            {
                OutputStreamService.SubscribeToOutputStream(outputCollection, ScriptStreamType.Output);
                OutputStreamService.SubscribeToOutputStream(shell.Streams.Information, ScriptStreamType.Information);
                OutputStreamService.SubscribeToOutputStream(shell.Streams.Progress, ScriptStreamType.Progress);
                OutputStreamService.SubscribeToOutputStream(shell.Streams.Warning, ScriptStreamType.Warning);
                OutputStreamService.SubscribeToOutputStream(shell.Streams.Error, ScriptStreamType.Error);
            }

            // Use Task.Factory to opt for the newer async/await keywords
            // Moves away from the old IAsyncResult functionality still used by the PowerShell API
            Task<PSDataCollection<PSObject>> shellTask = Task.Factory.FromAsync(
                shell.BeginInvoke<PSObject, PSObject>(null, outputCollection),
                shell.EndInvoke);

            PSDataCollection<PSObject> result = await shellTask.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            OutputStreamService?.AddOutput(ScriptStreamType.Warning, cancellationMessage);
        }
        catch (Exception e)
        {
            OutputStreamService?.AddOutput(ScriptStreamType.Error, e.Message);
        }
    }
}
