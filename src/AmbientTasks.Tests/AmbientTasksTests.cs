using System;
using System.Linq;
using System.Threading;
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

            var source = new TaskCompletionSource<object?>();
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

            var source = new TaskCompletionSource<object?>();
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
            var source = new TaskCompletionSource<object?>();
            source.SetCanceled();
            AmbientTasks.Add(source.Task);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_waits_for_added_task_with_no_context_to_succeed()
        {
            var source = new TaskCompletionSource<object?>();
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
            var source = new TaskCompletionSource<object?>();
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
            var source1 = new TaskCompletionSource<object?>();
            var source2 = new TaskCompletionSource<object?>();

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
            var source = new TaskCompletionSource<object?>();
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
            var source = new TaskCompletionSource<object?>();
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
            var source1 = new TaskCompletionSource<object?>();
            var source2 = new TaskCompletionSource<object?>();

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
            var source = new TaskCompletionSource<object?>();
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
            var source1 = new TaskCompletionSource<object?>();
            var source2 = new TaskCompletionSource<object?>();
            var source3 = new TaskCompletionSource<object?>();

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

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_should_have_single_AggregateException_with_all_exceptions_from_each_task()
        {
            var task1Exceptions = new[] { new Exception("Task 1 exception 1"), new Exception("Task 1 exception 2") };
            var task2Exceptions = new[] { new Exception("Task 2 exception 1"), new Exception("Task 2 exception 2") };
            var source1 = new TaskCompletionSource<object?>();
            var source2 = new TaskCompletionSource<object?>();
            AmbientTasks.Add(source1.Task);
            AmbientTasks.Add(source2.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();

            source1.SetException(task1Exceptions);
            source2.SetException(task2Exceptions);

            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);

            var aggregateException = waitAllTask.Exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<AggregateException>();

            aggregateException.InnerExceptions.ShouldBe(task1Exceptions.Concat(task2Exceptions));
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_should_have_single_AggregateException_with_all_exceptions_from_each_task_all_faulted_synchronously()
        {
            var task1Exceptions = new[] { new Exception("Task 1 exception 1"), new Exception("Task 1 exception 2") };
            var task2Exceptions = new[] { new Exception("Task 2 exception 1"), new Exception("Task 2 exception 2") };
            var source1 = new TaskCompletionSource<object?>();
            var source2 = new TaskCompletionSource<object?>();
            AmbientTasks.Add(source1.Task);
            AmbientTasks.Add(source2.Task);

            source1.SetException(task1Exceptions);
            source2.SetException(task2Exceptions);

            var waitAllTask = AmbientTasks.WaitAllAsync();

            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);

            var aggregateException = waitAllTask.Exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<AggregateException>();

            aggregateException.InnerExceptions.ShouldBe(task1Exceptions.Concat(task2Exceptions));
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void SynchronizationContext_that_throws_on_post_does_not_prevent_WaitAllAsync_completion()
        {
            var source = new TaskCompletionSource<object?>();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => throw new Exception()))
            {
                AmbientTasks.Add(source.Task);

                var waitAllTask = AmbientTasks.WaitAllAsync();
                source.SetException(new Exception());
                waitAllTask.Status.ShouldBe(TaskStatus.Faulted);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_receives_exceptions_from_synchronously_faulted_tasks()
        {
            var exception = new Exception();
            var watcher = new CallbackWatcher();

            AmbientTasks.BeginContext(ex =>
            {
                watcher.OnCallback();
                ex.ShouldBeSameAs(exception);
            });

            using (watcher.ExpectCallback())
                AmbientTasks.Add(Task.FromException(exception));
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_receives_exceptions_from_asynchronously_faulted_tasks()
        {
            var source = new TaskCompletionSource<object?>();
            var exception = new Exception();
            var watcher = new CallbackWatcher();

            AmbientTasks.BeginContext(ex =>
            {
                watcher.OnCallback();
                ex.ShouldBeSameAs(exception);
            });

            AmbientTasks.Add(source.Task);

            using (watcher.ExpectCallback())
                source.SetException(exception);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Handled_exceptions_do_not_appear_in_a_subsequent_call_to_WaitAllAsync()
        {
            AmbientTasks.BeginContext(handler => { });

            AmbientTasks.Add(Task.FromException(new Exception()));

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Handled_exceptions_do_not_appear_in_task_returned_from_WaitAllAsync_while_waiting_for_task()
        {
            var source = new TaskCompletionSource<object?>();
            var exception = new Exception();

            AmbientTasks.BeginContext(handler => { });

            AmbientTasks.Add(source.Task);

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            source.SetException(exception);

            waitAllTask.Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Handler_should_be_called_one_additional_time_to_handle_its_own_exception()
        {
            var taskException = new Exception();
            var handlerException = new Exception();
            var watcher = new CallbackWatcher();

            AmbientTasks.BeginContext(ex =>
            {
                watcher.OnCallback(out var callCount);

                if (callCount == 1) throw handlerException;

                ex.ShouldBe(handlerException);
            });

            using (watcher.ExpectCallback(count: 2))
                AmbientTasks.Add(Task.FromException(taskException));

            var waitAllTask = AmbientTasks.WaitAllAsync();

            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);

            var aggregateException = waitAllTask.Exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<AggregateException>();

            aggregateException.InnerExceptions.ShouldHaveSingleItem().ShouldBeSameAs(taskException);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_should_have_single_AggregateException_with_all_three_exceptions_when_handler_throws_exception_twice()
        {
            var taskException = new Exception();
            var handlerException1 = new Exception();
            var handlerException2 = new Exception();
            var watcher = new CallbackWatcher();

            AmbientTasks.BeginContext(ex =>
            {
                watcher.OnCallback(out var callCount);

                throw callCount == 1 ? handlerException1 : handlerException2;
            });

            using (watcher.ExpectCallback(count: 2))
                AmbientTasks.Add(Task.FromException(taskException));

            var waitAllTask = AmbientTasks.WaitAllAsync();

            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);
            var aggregateException = waitAllTask.Exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<AggregateException>();

            aggregateException.InnerExceptions.ShouldBe(new[] { taskException, handlerException1, handlerException2 });
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_is_not_executed_using_synchronization_context()
        {
            using (SynchronizationContextAssert.ExpectNoPost())
            {
                AmbientTasks.BeginContext(ex => { });

                AmbientTasks.Add(Task.FromException(new Exception()));

                using (Utils.WithTemporarySynchronizationContext(null))
                {
                    AmbientTasks.Add(Task.FromException(new Exception()));
                }
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_is_replaced_with_next_call()
        {
            AmbientTasks.BeginContext(ex => Assert.Fail("This handler should not be called."));

            var watcher = new CallbackWatcher();
            AmbientTasks.BeginContext(ex => watcher.OnCallback());

            using (watcher.ExpectCallback())
                AmbientTasks.Add(Task.FromException(new Exception()));
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_can_be_removed()
        {
            AmbientTasks.BeginContext(ex => Assert.Fail("This handler should not be called."));

            AmbientTasks.BeginContext();

            var exception = new Exception();
            var aggregateException = Should.Throw<AggregateException>(
                () => AmbientTasks.Add(Task.FromException(exception)));

            aggregateException.InnerExceptions.ShouldHaveSingleItem().ShouldBeSameAs(exception);

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.Faulted);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task BeginContext_handler_flows_into_Task_Run()
        {
            var watcher = new CallbackWatcher();
            AmbientTasks.BeginContext(ex => watcher.OnCallback());

            await Task.Run(() =>
            {
                using (watcher.ExpectCallback())
                    AmbientTasks.Add(Task.FromException(new Exception()));
            });
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task BeginContext_handler_flows_into_new_thread()
        {
            var source = new TaskCompletionSource<object?>();
            var watcher = new CallbackWatcher();

            var thread = new Thread(() =>
            {
                try
                {
                    using (watcher.ExpectCallback())
                        AmbientTasks.Add(Task.FromException(new Exception()));

                    source.SetResult(null);
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });

            AmbientTasks.BeginContext(ex => watcher.OnCallback());

            thread.Start();

            await source.Task;
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task BeginContext_handler_flows_into_ThreadPool_QueueUserWorkItem()
        {
            var watcher = new CallbackWatcher();
            AmbientTasks.BeginContext(ex => watcher.OnCallback());

            var source = new TaskCompletionSource<object?>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (watcher.ExpectCallback())
                        AmbientTasks.Add(Task.FromException(new Exception()));

                    source.SetResult(null);
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            }, null);

            await source.Task;
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task BeginContext_handler_flows_across_await()
        {
            var watcher = new CallbackWatcher();
            AmbientTasks.BeginContext(ex => watcher.OnCallback());

            await Task.Yield();

            using (watcher.ExpectCallback())
                AmbientTasks.Add(Task.FromException(new Exception()));
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static async Task BeginContext_handler_does_not_flow_from_inner_method_back_to_outer_method_after_await()
        {
            var watcher = new CallbackWatcher();
            AmbientTasks.BeginContext(ex => watcher.OnCallback());

            await InnerFunction();

            using (watcher.ExpectCallback())
                AmbientTasks.Add(Task.FromException(new Exception()));

            static async Task InnerFunction()
            {
                await Task.Yield();
                AmbientTasks.BeginContext(ex => Assert.Fail());
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void BeginContext_handler_can_be_called_recursively()
        {
            var exception1 = new Exception();
            var exception2 = new Exception();

            var depth = 0;
            var maxDepth = 0;

            AmbientTasks.BeginContext(ex =>
            {
                depth++;
                if (maxDepth < depth) maxDepth = depth;
                try
                {
                    if (ex == exception1) AmbientTasks.Add(Task.FromException(exception2));
                }
                finally
                {
                    depth--;
                }
            });

            AmbientTasks.Add(Task.FromException(exception1));

            maxDepth.ShouldBe(2);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void SynchronizationContext_that_throws_on_post_adds_exception_to_WaitAllAsync()
        {
            var source = new TaskCompletionSource<object?>();

            var postException = new Exception();
            var taskException = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => throw postException))
            {
                AmbientTasks.Add(source.Task);

                var waitAllTask = AmbientTasks.WaitAllAsync();
                source.SetException(taskException);
                waitAllTask.Status.ShouldBe(TaskStatus.Faulted);

                var aggregateException = waitAllTask.Exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<AggregateException>();
                aggregateException.InnerExceptions.ShouldBe(new[] { taskException, postException });
            }
        }
    }
}
