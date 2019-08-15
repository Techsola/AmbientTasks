using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Techsola
{
    public static class AmbientTasksAddFuncOverloadTests
    {
        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_null_func_is_noop()
        {
            AmbientTasks.Add((Func<Task>?)null);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Func_returning_null_task_is_noop()
        {
            AmbientTasks.Add(() => null!);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Func_returning_synchronously_successfully_completed_task_is_noop()
        {
            AmbientTasks.Add(() => Task.CompletedTask);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Func_returning_synchronously_canceled_task_is_noop()
        {
            var source = new TaskCompletionSource<object?>();
            source.SetCanceled();
            AmbientTasks.Add(() => source.Task);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Func_throwing_OperationCanceledException_is_noop()
        {
            AmbientTasks.Add(() => throw new OperationCanceledException());

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Func_throwing_TaskCanceledException_is_noop()
        {
            AmbientTasks.Add(() => throw new TaskCanceledException());

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_receives_exception_thrown_from_func()
        {
            var exception = new Exception();
            var watcher = new CallbackWatcher();

            AmbientTasks.BeginContext(ex =>
            {
                watcher.OnCallback();
                ex.ShouldBeSameAs(exception);
            });

            using (watcher.ExpectCallback())
                AmbientTasks.Add(() => throw exception);
        }
    }
}
