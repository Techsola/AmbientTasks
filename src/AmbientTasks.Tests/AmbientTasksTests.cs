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
        public static void Adding_synchronously_faulted_task_with_no_context_throws_AggregateException_synchronously()
        {
            var exception = new Exception();

            var aggregateException = Should.Throw<AggregateException>(
                () => AmbientTasks.Add(Task.FromException(exception)));

            aggregateException.InnerExceptions.ShouldBe(new[] { exception });
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Adding_synchronously_faulted_task_with_no_context_throws_AggregateException_on_current_SynchronizationContext()
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
        public static void Adding_synchronously_faulted_task_with_multiple_exceptions_and_no_context_throws_AggregateException_synchronously_with_all_exceptions()
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
        public static void Adding_synchronously_faulted_task_with_multiple_exceptions_and_no_context_throws_AggregateException_on_current_SynchronizationContexts()
        {
            var exceptions = new[]
            {
                new Exception(),
                new Exception()
            };

            var source = new TaskCompletionSource<object>();
            source.SetException(exceptions);

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction =>
            {
                var aggregateException = Should.Throw<AggregateException>(postedAction);

                aggregateException.InnerExceptions.ShouldBe(exceptions);
            }))
            {
                AmbientTasks.Add(source.Task);
            }
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
        public static void WaitAllAsync_should_reset_after_fault_is_displayed_in_previously_returned_task()
        {
            Should.Throw<AggregateException>(
                () => AmbientTasks.Add(Task.FromException(new Exception())));

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.Faulted);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_should_fault_after_adding_synchronously_failed_task_with_no_context_when_on_SynchronizationContext()
        {
            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => { }))
            {
                AmbientTasks.Add(Task.FromException(new Exception()));

                AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.Faulted);
            }
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

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_waits_for_added_task_with_no_context_to_succeed()
        {
            var source = new TaskCompletionSource<object>();
            AmbientTasks.Add(source.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            source.SetResult(null);
            waitAllTask.Status.ShouldBe(TaskStatus.RanToCompletion);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_waits_for_added_task_with_no_context_to_cancel()
        {
            var source = new TaskCompletionSource<object>();
            AmbientTasks.Add(source.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            source.SetCanceled();
            waitAllTask.Status.ShouldBe(TaskStatus.RanToCompletion);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_does_not_reset_until_all_tasks_have_completed()
        {
            var source1 = new TaskCompletionSource<object>();
            var source2 = new TaskCompletionSource<object>();

            AmbientTasks.Add(source1.Task);
            var waitTaskSeenAfterFirstAdd = AmbientTasks.WaitAllAsync();
            waitTaskSeenAfterFirstAdd.IsCompleted.ShouldBeFalse();

            // Should not reset on next call
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitTaskSeenAfterFirstAdd);

            AmbientTasks.Add(source2.Task);
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitTaskSeenAfterFirstAdd);

            // Should not reset on next call
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitTaskSeenAfterFirstAdd);

            source1.SetResult(null);
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitTaskSeenAfterFirstAdd);

            // Should not reset on next call
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitTaskSeenAfterFirstAdd);

            source2.SetResult(null);
            waitTaskSeenAfterFirstAdd.Status.ShouldBe(TaskStatus.RanToCompletion);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_waits_for_added_task_with_no_context_to_fault()
        {
            var source = new TaskCompletionSource<object>();
            AmbientTasks.Add(source.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            source.SetException(new Exception());
            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_waits_for_added_task_with_no_context_and_throws_exception_on_current_SynchronizationContext()
        {
            var source = new TaskCompletionSource<object>();
            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction =>
            {
                var aggregateException = Should.Throw<AggregateException>(postedAction);

                aggregateException.InnerExceptions.ShouldBe(new[] { exception });
            }))
            {
                AmbientTasks.Add(source.Task);

                var waitAllTask = AmbientTasks.WaitAllAsync();
                waitAllTask.IsCompleted.ShouldBeFalse();

                source.SetException(exception);

                waitAllTask.Status.ShouldBe(TaskStatus.Faulted);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_resets_after_overlapping_tasks_fail()
        {
            var source1 = new TaskCompletionSource<object>();
            var source2 = new TaskCompletionSource<object>();

            AmbientTasks.Add(source1.Task);
            AmbientTasks.Add(source2.Task);

            var waitAllTaskBeforeAllExceptions = AmbientTasks.WaitAllAsync();

            source1.SetException(new Exception());
            source2.SetException(new Exception());

            waitAllTaskBeforeAllExceptions.Status.ShouldBe(TaskStatus.Faulted);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task WaitAllAsync_should_throw_AggregateException_when_awaited()
        {
            var source = new TaskCompletionSource<object>();
            AmbientTasks.Add(source.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();

            var exception = new Exception();
            source.SetException(exception);

            var aggregateException = await Should.ThrowAsync<AggregateException>(waitAllTask);
            aggregateException.InnerExceptions.ShouldBe(new[] { exception });
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_does_not_return_with_fault_from_added_test_until_last_task_completes()
        {
            var source1 = new TaskCompletionSource<object>();
            var source2 = new TaskCompletionSource<object>();
            var source3 = new TaskCompletionSource<object>();

            AmbientTasks.Add(source1.Task);
            AmbientTasks.Add(source2.Task);
            AmbientTasks.Add(source3.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();

            source1.SetException(new Exception());
            waitAllTask.IsCompleted.ShouldBeFalse();
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitAllTask);

            source2.SetResult(null);
            waitAllTask.IsCompleted.ShouldBeFalse();
            AmbientTasks.WaitAllAsync().ShouldBeSameAs(waitAllTask);

            source3.SetResult(null);
            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }
    }
}
