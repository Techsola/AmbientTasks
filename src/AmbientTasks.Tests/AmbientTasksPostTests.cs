using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Techsola
{
    public static class AmbientTasksPostTests
    {
        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Passed_delegate_may_be_null([Values] bool actionOverload)
        {
            if (actionOverload)
                AmbientTasks.Post(null);
            else
                AmbientTasks.Post(null, state: null);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Passed_synchronization_context_may_not_be_null([Values] bool actionOverload)
        {
            if (actionOverload)
            {
                Should.Throw<ArgumentNullException>(
                        () => AmbientTasks.Post(synchronizationContext: null, () => { }))
                    .ParamName.ShouldBe("synchronizationContext");
            }
            else
            {
                Should.Throw<ArgumentNullException>(
                        () => AmbientTasks.Post(synchronizationContext: null, state => { }, state: null))
                    .ParamName.ShouldBe("synchronizationContext");
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task Executes_asynchronously_if_current_synchronization_context_is_null([Values] bool actionOverload)
        {
            var source = new TaskCompletionSource<Thread>();

            using (Utils.WithTemporarySynchronizationContext(null))
            {
                if (actionOverload)
                    AmbientTasks.Post(() => source.SetResult(Thread.CurrentThread));
                else
                    AmbientTasks.Post(_ => source.SetResult(Thread.CurrentThread), null);

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
