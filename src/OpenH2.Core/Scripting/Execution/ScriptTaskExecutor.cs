﻿using OpenH2.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpenH2.Core.Scripting.Execution
{
    public class ScriptTaskExecutor : BaseScriptExecutor
    {
        private delegate Task OrchestratedScript();
        public ulong CurrentTick { get; private set; } = 0;

        private ExecutionState[] executionStates = Array.Empty<ExecutionState>();
        private AsyncLocal<string> currentScript = new AsyncLocal<string>();

        public void Setup(ScenarioScriptBase scripts)
        {
            var scriptMethods = scripts.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<ScriptMethodAttribute>() != null)
                .ToArray();

            var execStates = new List<ExecutionState>(scriptMethods.Length);

            foreach (var script in scriptMethods)
            {
                var attr = script.GetCustomAttribute<ScriptMethodAttribute>();

                var initialStatus = attr.Lifecycle switch
                {
                    Lifecycle.Startup => ScriptStatus.RunOnce,
                    Lifecycle.Dormant => ScriptStatus.Sleeping,
                    Lifecycle.Continuous => ScriptStatus.RunContinuous,
                    Lifecycle.Static => ScriptStatus.Terminated,
                    Lifecycle.Stub => ScriptStatus.Terminated,
                    Lifecycle.CommandScript => ScriptStatus.Terminated,
                    _ => throw new NotImplementedException()
                };


                var execState = new ExecutionState()
                {
                    Description = script.Name,
                    Status = initialStatus,
                    Task = null,
                    SleepUntil = DateTimeOffset.MaxValue
                };

                OrchestratedScript func;

                // Top level scripts shouldn't have a return value, but we'll wrap the func
                // in an expression to allow us to invoke it if necessary
                if(script.ReturnType.IsGenericType && script.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    func = Expression.Lambda<OrchestratedScript>(
                        Expression.Convert(
                            Expression.Call(Expression.Constant(scripts), script),
                            typeof(Task))).Compile();
                }
                else
                {
                    func = (OrchestratedScript)script.CreateDelegate(typeof(OrchestratedScript), scripts);
                }

                execState.Func = async () =>
                {
                    await Task.Yield();
                    currentScript.Value = execState.Description;
                    await func();
                };

                execStates.Add(execState);
            }

            this.executionStates = execStates.ToArray();
        }

        public override void Execute()
        {
            CurrentTick++;

            lock(tasks)
            {
                var currentTaskNode = tasks.First;
                while (currentTaskNode != null)
                {
                    var tNode = currentTaskNode;
                    var t = tNode.Value;
                    currentTaskNode = currentTaskNode.Next;
                    if (t.expiry < CurrentTick)
                    {
                        t.tcs.TrySetResult(null);
                        tasks.Remove(tNode);
                    }
                }
            }

            for (var i = 0; i < executionStates.Length; i++)
            {
                var state = executionStates[i];

                if(state.Status == ScriptStatus.RunContinuous && (state.Task?.IsCompleted ?? true))
                {
                    state.Task = state.Func();
                }
                if (state.Status == ScriptStatus.RunOnce)
                {
                    if(state.Task == null)
                    {
                        state.Task = state.Func();
                    }
                    else if(state.Task.IsCompleted)
                    {
                        state.Status = ScriptStatus.Terminated;
                    }
                }
                if (state.Status == ScriptStatus.Sleeping && state.SleepUntil < DateTimeOffset.UtcNow)
                {
                    Logger.LogInfo($"[SCRIPT] ({state.Description}) - waking up");
                    state.Status = ScriptStatus.RunOnce;
                    state.Task = state.Func();
                }

                executionStates[i] = state;
            }
        }

        public override void SetStatus(ScriptStatus desiredStatus)
        {
            var currentName = currentScript.Value;

            for (var i = 0; i < executionStates.Length; i++)
            {
                var state = executionStates[i];

                if (state.Description == currentName)
                {
                    SetStatus((ushort)i, desiredStatus);
                }

                executionStates[i] = state;
            }
        }

        public override void SetStatus(ushort methodId, ScriptStatus desiredStatus)
        {
            ref var state = ref executionStates[methodId];

            if (state.Status == ScriptStatus.Terminated)
            {
                Logger.Log($"[SCRIPT] Trying to set terminated lifecycle to {desiredStatus}", Logger.Color.Red);
                return;
            }

            state.Status = desiredStatus;
            Logger.LogInfo($"[SCRIPT] ({state.Description}) -> {desiredStatus}");
        }

        private struct ExecutionState
        {
            public string Description;
            public OrchestratedScript Func;
            public ScriptStatus Status;
            public DateTimeOffset SleepUntil;
            public Task? Task;
        }

        /// <summary>
        /// Creates a task that will take the specified number of updates to complete
        /// </summary>
        public override ValueTask Delay(int ticks)
        {
            var t = new TaskCompletionSource<object>();

            lock(tasks)
            {
                tasks.AddLast(new EngineTickTask { expiry = CurrentTick + (ulong)ticks, tcs = t });
            }

            return new ValueTask(t.Task);
        }

        public override ValueTask Delay(ushort methodId, int ticks)
        {
            // Sleeping another script is not supported
            return new ValueTask(); 
        }

        private LinkedList<EngineTickTask> tasks = new LinkedList<EngineTickTask>();

        private struct EngineTickTask { public ulong expiry; public TaskCompletionSource<object> tcs; }
    }
}
