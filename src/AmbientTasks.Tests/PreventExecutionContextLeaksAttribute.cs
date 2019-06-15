using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Techsola;

[assembly: RequireOnAllTestMethods(typeof(PreventExecutionContextLeaksAttribute))]

namespace Techsola
{
    /// <summary>
    /// Workaround for https://github.com/nunit/nunit/issues/3283.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PreventExecutionContextLeaksAttribute : Attribute, IWrapSetUpTearDown
    {
        public TestCommand Wrap(TestCommand command) => new ExecuteInIsolatedExecutionContextCommand(command);

        private sealed class ExecuteInIsolatedExecutionContextCommand : DelegatingTestCommand
        {
            public ExecuteInIsolatedExecutionContextCommand(TestCommand innerCommand) : base(innerCommand)
            {
            }

            [DebuggerNonUserCode]
            public override TestResult Execute(TestExecutionContext context)
            {
                using (var copy = ExecutionContext.Capture().CreateCopy())
                {
                    var returnValue = new StrongBox<TestResult>();
                    ExecutionContext.Run(copy, Execute, state: (context, returnValue));
                    return returnValue.Value;
                }
            }

            [DebuggerNonUserCode]
            private void Execute(object state)
            {
                var (context, returnValue) = ((TestExecutionContext, StrongBox<TestResult>))state;

                returnValue.Value = innerCommand.Execute(context);
            }
        }
    }
}
