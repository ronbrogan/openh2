﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenH2.Core.Scripting.Execution
{
    public enum ScriptStatus
    {
        Sleeping,
        RunContinuous,
        RunOnce,
        Scheduled,
        Terminated
    }
}
