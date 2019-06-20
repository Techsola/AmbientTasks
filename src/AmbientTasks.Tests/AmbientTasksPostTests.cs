using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Techsola
{
    public static class AmbientTasksPostTests
    {
        public enum PostOverload
        {
            Action,
            SendOrPostCallback
        }

        private static void AmbientTasksPost(PostOverload overload, Action action)
        {
            switch (overload)
            {
                case PostOverload.Action:
                    AmbientTasks.Post(action);
                    break;
                case PostOverload.SendOrPostCallback:
                    AmbientTasks.Post(state => ((Action)state).Invoke(), state: action);
                    break;
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Passed_delegate_may_be_null([Values] PostOverload overload)
        {
            switch (overload)
            {
                case PostOverload.Action:
                    AmbientTasks.Post(null);
                    break;
                case PostOverload.SendOrPostCallback:
                    AmbientTasks.Post(null, state: null);
                    break;
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Passed_synchronization_context_may_not_be_null([Values] PostOverload overload)
        {
            switch (overload)
            {
                case PostOverload.Action:
                    Should.Throw<ArgumentNullException>(
                            () => AmbientTasks.Post(synchronizationContext: null, () => { }))
                        .ParamName.ShouldBe("synchronizationContext");
                    break;
                case PostOverload.SendOrPostCallback:
                    Should.Throw<ArgumentNullException>(
                            () => AmbientTasks.Post(synchronizationContext: null, state => { }, state: null))
                        .ParamName.ShouldBe("synchronizationContext");
                    break;
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task Executes_asynchronously_if_current_synchronization_context_is_null([Values] PostOverload overload)
        {
            var source = new TaskCompletionSource<Thread>();

            using (Utils.WithTemporarySynchronizationContext(null))
            {
                AmbientTasksPost(overload, () => source.SetResult(Thread.CurrentThread));

                if (source.Task.Status == TaskStatus.RanToCompletion)
                {
                    var threadUsed = source.Task.Result;
                    threadUsed.ShouldNotBeSameAs(Thread.CurrentThread);
                }
            }

            await source.Task;
        }
    }
}
