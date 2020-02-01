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
            SendOrPostCallback,
            AsyncAction
        }

        private static void AmbientTasksPost(PostOverload overload, Action action)
        {
            switch (overload)
            {
                case PostOverload.Action:
                    AmbientTasks.Post(action);
                    break;
                case PostOverload.SendOrPostCallback:
                    AmbientTasks.Post(state => ((Action)state!).Invoke(), state: action);
                    break;
                case PostOverload.AsyncAction:
                    AmbientTasks.Post(() =>
                    {
                        action.Invoke();
                        return Task.CompletedTask;
                    });
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Passed_delegate_may_be_null([Values] PostOverload overload)
        {
            switch (overload)
            {
                case PostOverload.Action:
                    AmbientTasks.Post((Action?)null);
                    break;
                case PostOverload.SendOrPostCallback:
                    AmbientTasks.Post(null, state: null);
                    break;
                case PostOverload.AsyncAction:
                    AmbientTasks.Post((Func<Task>?)null);
                    break;
                default:
                    throw new NotImplementedException();
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
                            () => AmbientTasks.Post(synchronizationContext: null!, (Action?)null))
                        .ParamName.ShouldBe("synchronizationContext");
                    break;
                case PostOverload.SendOrPostCallback:
                    Should.Throw<ArgumentNullException>(
                            () => AmbientTasks.Post(synchronizationContext: null!, null, state: null))
                        .ParamName.ShouldBe("synchronizationContext");
                    break;
                case PostOverload.AsyncAction:
                    Should.Throw<ArgumentNullException>(
                            () => AmbientTasks.Post(synchronizationContext: null!, (Func<Task>?)null))
                        .ParamName.ShouldBe("synchronizationContext");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Passed_synchronization_context_is_used_for_post([Values] PostOverload overload)
        {
            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => { }))
            {
                var contextToUse = SynchronizationContext.Current!;

                using (SynchronizationContextAssert.ExpectNoPost())
                {
                    switch (overload)
                    {
                        case PostOverload.Action:
                            AmbientTasks.Post(contextToUse, () => { });
                            break;
                        case PostOverload.SendOrPostCallback:
                            AmbientTasks.Post(contextToUse, state => { }, state: null);
                            break;
                        case PostOverload.AsyncAction:
                            AmbientTasks.Post(contextToUse, () => Task.CompletedTask);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
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

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_SynchronizationContext_post_before_invoking_delegate_is_not_handled_when_there_is_no_BeginContext_handler([Values] PostOverload overload)
        {
            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => throw exception))
            {
                Should.Throw<Exception>(() => AmbientTasksPost(overload, () => { })).ShouldBeSameAs(exception);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_SynchronizationContext_post_before_invoking_delegate_is_not_handled_when_there_is_a_BeginContext_handler([Values] PostOverload overload)
        {
            AmbientTasks.BeginContext(ex => { });

            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => throw exception))
            {
                Should.Throw<Exception>(() => AmbientTasksPost(overload, () => { })).ShouldBeSameAs(exception);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_SynchronizationContext_post_after_invoking_delegate_is_not_handled_when_there_is_no_BeginContext_handler([Values] PostOverload overload)
        {
            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction =>
            {
                postedAction.Invoke();
                throw exception;
            }))
            {
                Should.Throw<Exception>(() => AmbientTasksPost(overload, () => { })).ShouldBeSameAs(exception);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_SynchronizationContext_post_after_invoking_delegate_is_not_handled_when_there_is_a_BeginContext_handler([Values] PostOverload overload)
        {
            AmbientTasks.BeginContext(ex => { });

            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction =>
            {
                postedAction.Invoke();
                throw exception;
            }))
            {
                Should.Throw<Exception>(() => AmbientTasksPost(overload, () => { })).ShouldBeSameAs(exception);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_user_delegate_is_thrown_on_SynchronizationContext_when_there_is_no_BeginContext_handler([Values] PostOverload overload)
        {
            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction =>
            {
                Should.Throw<Exception>(postedAction).ShouldBeSameAs(exception);
            }))
            {
                AmbientTasksPost(overload, () => throw exception);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_user_delegate_is_not_thrown_on_SynchronizationContext_when_there_is_a_BeginContext_handler([Values] PostOverload overload)
        {
            AmbientTasks.BeginContext(ex => { });

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => postedAction.Invoke()))
            {
                AmbientTasksPost(overload, () => throw new Exception());
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_user_delegate_is_handled_by_BeginContext_handler([Values] PostOverload overload)
        {
            var exception = new Exception();
            var watcher = new CallbackWatcher();

            AmbientTasks.BeginContext(ex =>
            {
                watcher.OnCallback();
                ex.ShouldBeSameAs(exception);
            });

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => postedAction.Invoke()))
            {
                using (watcher.ExpectCallback())
                    AmbientTasksPost(overload, () => throw exception);
            }
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_user_delegate_is_in_task_from_next_call_to_WaitAllAsync_when_there_is_no_BeginContext_handler([Values] PostOverload overload)
        {
            var exception = new Exception();

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => postedAction.Invoke()))
            {
                Should.Throw<Exception>(() => AmbientTasksPost(overload, () => throw exception));
            }

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.Status.ShouldBe(TaskStatus.Faulted);

            var aggregateException = waitAllTask.Exception!.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<AggregateException>();

            aggregateException.InnerExceptions.ShouldHaveSingleItem().ShouldBeSameAs(exception);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Exception_from_user_delegate_is_not_in_task_from_next_call_to_WaitAllAsync_when_there_is_a_BeginContext_handler([Values] PostOverload overload)
        {
            AmbientTasks.BeginContext(ex => { });

            using (SynchronizationContextAssert.ExpectSinglePost(postedAction => postedAction.Invoke()))
            {
                AmbientTasksPost(overload, () => throw new Exception());
            }

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_waits_for_SynchronizationContext_Post_to_invoke_delegate_before_completing([Values] PostOverload overload)
        {
            var postedAction = (Action?)null;

            using (SynchronizationContextAssert.ExpectSinglePost(p => postedAction = p))
            {
                AmbientTasksPost(overload, () => { });
            }

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            postedAction.ShouldNotBeNull();
            postedAction!.Invoke();

            waitAllTask.Status.ShouldBe(TaskStatus.RanToCompletion);
            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_does_not_complete_until_async_task_added_by_user_delegate_completes([Values] PostOverload overload)
        {
            var source = new TaskCompletionSource<object?>();
            var postedAction = (Action?)null;

            using (SynchronizationContextAssert.ExpectSinglePost(p => postedAction = p))
            {
                AmbientTasksPost(overload, () =>
                {
                    AmbientTasks.Add(source.Task);
                });
            }

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            postedAction.ShouldNotBeNull();
            postedAction!.Invoke();

            waitAllTask.IsCompleted.ShouldBeFalse();

            source.SetResult(null);

            waitAllTask.Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void WaitAllAsync_does_not_complete_until_async_task_returned_by_user_delegate_completes()
        {
            var source = new TaskCompletionSource<object?>();
            var postedAction = (Action?)null;

            using (SynchronizationContextAssert.ExpectSinglePost(p => postedAction = p))
            {
                AmbientTasks.Post(() => source.Task);
            }

            var waitAllTask = AmbientTasks.WaitAllAsync();
            waitAllTask.IsCompleted.ShouldBeFalse();

            postedAction.ShouldNotBeNull();
            postedAction!.Invoke();

            waitAllTask.IsCompleted.ShouldBeFalse();

            source.SetResult(null);

            waitAllTask.Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Delegate_is_abandoned_if_SynchronizationContext_post_throws_before_invoking_delegate([Values] PostOverload overload)
        {
            var postedAction = (Action?)null;

            using (SynchronizationContextAssert.ExpectSinglePost(p =>
            {
                postedAction = p;
                throw new Exception();
            }))
            {
                Should.Throw<Exception>(() => AmbientTasksPost(overload, () => Assert.Fail("The delegate should be abandoned.")));
            }

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);

            postedAction.ShouldNotBeNull();
            postedAction!.Invoke();

            AmbientTasks.WaitAllAsync().Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Subsequent_invocations_by_SynchronizationContext_are_ignored_when_successful([Values] PostOverload overload)
        {
            var postedAction = (Action?)null;
            var callCount = 0;

            using (SynchronizationContextAssert.ExpectSinglePost(p =>
            {
                postedAction = p;
            }))
            {
                AmbientTasksPost(overload, () => callCount++);
            }

            callCount.ShouldBe(0);
            postedAction.ShouldNotBeNull();
            postedAction!.Invoke();
            callCount.ShouldBe(1);

            postedAction.Invoke();

            callCount.ShouldBe(1);
        }

        [Test]
        [PreventExecutionContextLeaks] // Workaround for https://github.com/nunit/nunit/issues/3283
        public static void Subsequent_invocations_by_SynchronizationContext_are_ignored_when_user_code_throws([Values] PostOverload overload)
        {
            var postedAction = (Action?)null;
            var callCount = 0;

            using (SynchronizationContextAssert.ExpectSinglePost(p =>
            {
                postedAction = p;
            }))
            {
                AmbientTasksPost(overload, () =>
                {
                    callCount++;
                    throw new Exception();
                });
            }

            callCount.ShouldBe(0);
            Should.Throw<Exception>(postedAction);
            callCount.ShouldBe(1);

            postedAction.ShouldNotBeNull();
            postedAction!.Invoke();

            callCount.ShouldBe(1);
        }
    }
}
