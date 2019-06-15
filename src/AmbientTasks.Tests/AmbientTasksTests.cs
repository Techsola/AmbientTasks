using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Techsola
{
    public static class AmbientTasksTests
    {
        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_synchronously_failed_task_with_no_context_throws_AggregateException_synchronously()
        {
            var exception = new Exception();

            var aggregateException = Should.Throw<AggregateException>(
                () => AmbientTasks.Add(Task.FromException(exception)));

            aggregateException.InnerExceptions.ShouldBe(new[] { exception });
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_synchronously_failed_task_with_no_context_throws_AggregateException_on_current_SynchronizationContext()
        {
            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction =>
            {
                var aggregateException = Should.Throw<AggregateException>(postedAction);

                aggregateException.InnerExceptions.ShouldBe(new[] { exception });
            }))
            {
                AmbientTasks.Add(Task.FromException(exception));
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_synchronously_failed_task_with_multiple_exceptions_and_no_context_throws_AggregateException_synchronously_with_all_exceptions()
        {
            var exceptions = new[]
            {
                new Exception(),
                new Exception()
            };

            var source = new TaskCompletionSource<object>();
            source.SetException(exceptions);

            var aggregateException = Should.Throw<AggregateException>(() => AmbientTasks.Add(source.Task));

            aggregateException.InnerExceptions.ShouldBe(exceptions);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_should_fault_after_adding_synchronously_failed_task_with_no_context()
        {
            Should.Throw<AggregateException>(
                () => AmbientTasks.Add(Task.FromException(new Exception())));

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.Faulted);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_null_task_is_noop()
        {
            AmbientTasks.Add(null);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_synchronously_successfully_completed_task_is_noop()
        {
            AmbientTasks.Add(Task.CompletedTask);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_synchronously_canceled_task_is_noop()
        {
            var source = new TaskCompletionSource<object>();
            source.SetCanceled();
            AmbientTasks.Add(source.Task);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }
    }
}
