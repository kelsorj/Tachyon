using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Workflow.Runtime;

namespace BioNex.Shared.Utils
{
    // implementation taken from http://www.yoda.arachsys.com/csharp/singleton.html
    public sealed class WorkflowRuntimeSingleton
    {
        public WorkflowRuntime workflow_runtime;

        WorkflowRuntimeSingleton()
        {
            workflow_runtime = new WorkflowRuntime();
        }

        public static WorkflowRuntimeSingleton Instance
        {
            get { return Nested.Instance; }
        }

        private class Nested
        {
            static Nested()
            {
            }

            internal static readonly WorkflowRuntimeSingleton Instance = new WorkflowRuntimeSingleton();
        }
    }
}
