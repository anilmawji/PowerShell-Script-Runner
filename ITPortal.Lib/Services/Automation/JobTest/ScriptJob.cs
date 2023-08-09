﻿using ITPortal.Lib.Services.Automation.Script;

namespace ITPortal.Lib.Services.Automation.JobTest;

public class ScriptJob
{
    public string Id { get; set; }
    public AutomationScript Script { get; set; }
    public string? Description { get; set; }
    public ScriptJobState State { get; private set; }
    public DateTime? ExecutionTime { get; private set; }

    public ScriptJob(string id, AutomationScript script)
    {
        Id = id;
        Script = script;
    }

    public ScriptJob(string id, AutomationScript script, string description) : this(id, script)
    {
        Description = description;
    }
}
