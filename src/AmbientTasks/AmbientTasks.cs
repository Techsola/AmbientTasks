using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Techsola
{
    /// <summary>
    /// Enables scoped completion tracking and error handling of tasks as an alternative to fire-and-forget and <c>async
    /// void</c>. Easy to produce and consume, and test-friendly.
    /// </summary>
    public static partial class AmbientTasks
    {
        private static readonly AsyncLocal<AmbientTaskContext> Context = new AsyncLocal<AmbientTaskContext>();

        private static AmbientTaskContext CurrentContext => Context.Value ??= new AmbientTaskContext(exceptionHandler: null);

        /// <summary>
        /// <para>
        /// Replaces the current async-local scope with a new scope which has its own exception handler and isolated set
        /// of tracked tasks.
        /// </para>
        /// <para>If <paramref name="exceptionHandler"/> is <see langword="null"/>, exceptions will be left uncaught. In
        /// the case of tracked <see cref="Task"/> objects, the exception will be rethrown on the synchronization
        /// context which began tracking it.
        /// </para>
        /// </summary>
        public static void BeginContext(Action<Exception>? exceptionHandler = null)
        {
            Context.Value = new AmbientTaskContext(exceptionHandler);
        }

        /// <summary>
        /// Waits until all tracked tasks are complete. Any exceptions that were not handled, including exceptions
        /// thrown by an exception handler, will be included as inner exceptions of the <see cref="Task.Exception"/>
        /// property.
        /// </summary>
        public static Task WaitAllAsync() => CurrentContext.WaitAllAsync();

        /// <summary>
        /// <para>
        /// Begins tracking a <see cref="Task"/> so that any exception is handled and so that <see cref="WaitAllAsync"/>
        /// waits for its completion.
        /// </para>
        /// <para>
        /// Once passed to this method, a task’s exception will never be unobserved. If the task faults or is already
        /// faulted and an exception handler is currently registered (see <see cref="BeginContext"/>), the handler will
        /// receive the task’s <see cref="AggregateException"/>. If no handler has been registered, the <see
        /// cref="AggregateException"/> will be rethrown on the <see cref="SynchronizationContext"/> that was current
        /// when <see cref="Add(Task)"/> was called. (If there was no synchronization context, it will be rethrown
        /// immediately by a continuation requesting <see cref="TaskContinuationOptions.ExecuteSynchronously"/>.)
        /// </para>
        /// </summary>
        public static void Add(Task? task)
        {
            if (task is null) return;

            switch (task.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.RanToCompletion:
                    break;

                case TaskStatus.Faulted:
                    OnTaskCompleted(task, state: (CurrentContext, SynchronizationContext.Current, taskWasStarted: false));
                    break;

                default:
                    var context = CurrentContext;
                    context.StartTask();
                    task.ContinueWith(
                        OnTaskCompleted,
                        state: (context, SynchronizationContext.Current, taskWasStarted: true),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    break;
            }
        }

        private static void OnTaskCompleted(Task completedTask, object? state)
        {
            var (context, addSynchronizationContext, taskWasStarted) = ((AmbientTaskContext, SynchronizationContext, bool))state!;
            try
            {
                if (completedTask.IsFaulted)
                {
                    if (!context.RecordAndTrySuppress(completedTask.Exception!.InnerExceptions))
                    {
                        var exceptionInfo = ExceptionDispatchInfo.Capture(completedTask.Exception);

                        if (addSynchronizationContext is null)
                        {
                            OnTaskFaultWithoutHandler(exceptionInfo);
                        }
                        else
                        {
                            try
                            {
                                addSynchronizationContext.Post(OnTaskFaultWithoutHandler, state: exceptionInfo);
                            }
                            catch (Exception postException) when (context.RecordAndTrySuppress(new[] { postException }))
                            {
                            }
                        }
                    }
                }
            }
            finally
            {
                if (taskWasStarted) context.EndTask();
            }
        }

        private static void OnTaskFaultWithoutHandler(object? state)
        {
            ((ExceptionDispatchInfo)state!).Throw();
        }

        /// <summary>
        /// <para>
        /// Invokes a delegate immediately and begins tracking the returned <see cref="Task"/> so that any exception is
        /// handled and so that <see cref="WaitAllAsync"/> waits for its completion. If the delegate throws rather than
        /// returning a task, the exception is wrapped in a <see cref="Task"/> instance and treated the same as if the
        /// task had been returned from the delegate.
        /// </para>
        /// <para>
        /// Once passed to this method, a task’s exception will never be unobserved. If the task faults or is already
        /// faulted and an exception handler is currently registered (see <see cref="BeginContext"/>), the handler will
        /// receive the task’s <see cref="AggregateException"/>. If no handler has been registered, the <see
        /// cref="AggregateException"/> will be rethrown on the <see cref="SynchronizationContext"/> that was current
        /// when <see cref="Add(Func{Task})"/> was called. (If there was no synchronization context, it will be rethrown
        /// immediately by a continuation requesting <see cref="TaskContinuationOptions.ExecuteSynchronously"/>.)
        /// </para>
        /// </summary>
        public static void Add(Func<Task>? functionToInvoke)
        {
            if (functionToInvoke is null) return;

            Add(InvokeWithExceptionsWrapped(functionToInvoke));
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions must be wrapped.")]
        private static Task InvokeWithExceptionsWrapped(Func<Task> function)
        {
            try
            {
                return function.Invoke();
            }
            catch (OperationCanceledException)
            {
                var source = new TaskCompletionSource<object?>();
                source.SetCanceled();
                return source.Task;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        /// <summary>
        /// <para>
        /// Executes the specified delegate on the current <see cref="SynchronizationContext"/> while tracking so that
        /// any exception is handled and so that <see cref="WaitAllAsync"/> waits for its completion.
        /// </para>
        /// <para>
        /// A default <see cref="SynchronizationContext"/> is installed if the current one is <see langword="null"/>.
        /// </para>
        /// <para>
        /// If an exception handler has been registered (see <see cref="BeginContext"/>), any exception will be caught
        /// and routed to the handler instead of <see cref="WaitAllAsync"/>. If no handler has been registered, the
        /// exception will not be caught even though it will be recorded and thrown by <see cref="WaitAllAsync"/>.
        /// </para>
        /// </summary>
        public static void Post(SendOrPostCallback? d, object? state)
        {
            // Install a default synchronization context if one does not exist
            Post(AsyncOperationManager.SynchronizationContext, d, state);
        }

        /// <summary>
        /// <para>
        /// Executes the specified delegate on the current <see cref="SynchronizationContext"/> while tracking so that
        /// any exception is handled and so that <see cref="WaitAllAsync"/> waits for its completion.
        /// </para>
        /// <para>
        /// <see cref="ArgumentNullException"/> is thrown if <paramref name="synchronizationContext"/> is <see
        /// langword="null"/>.
        /// </para>
        /// <para>
        /// If an exception handler has been registered (see <see cref="BeginContext"/>), any exception will be caught
        /// and routed to the handler instead of <see cref="WaitAllAsync"/>. If no handler has been registered, the
        /// exception will not be caught even though it will be recorded and thrown by <see cref="WaitAllAsync"/>.
        /// </para>
        /// </summary>
        public static void Post(SynchronizationContext synchronizationContext, SendOrPostCallback? d, object? state)
        {
            if (synchronizationContext is null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            if (d is null) return;

            var context = CurrentContext;
            context.StartTask();

            var closure = new PostClosure<(SendOrPostCallback, object?)>(context, (d, state));

            var postReturned = false;
            try
            {
                synchronizationContext.Post(OnPost, closure);
                postReturned = true;
            }
            finally
            {
                if (!postReturned && closure.TryClaimInvocation())
                {
                    context.EndTask();
                }
            }
        }

        private static void OnPost(object? state)
        {
            var closure = (PostClosure<(SendOrPostCallback d, object? state)>)state!;
            if (!closure.TryClaimInvocation()) return;
            try
            {
                closure.State.d.Invoke(closure.State.state);
            }
            catch (Exception ex) when (closure.Context.RecordAndTrySuppress(new[] { ex }))
            {
            }
            finally
            {
                closure.Context.EndTask();
            }
        }

        /// <summary>
        /// <para>
        /// Executes the specified delegate on the current <see cref="SynchronizationContext"/> while tracking so that
        /// any exception is handled and so that <see cref="WaitAllAsync"/> waits for its completion.
        /// </para>
        /// <para>
        /// A default <see cref="SynchronizationContext"/> is installed if the current one is <see langword="null"/>.
        /// </para>
        /// <para>
        /// If an exception handler has been registered (see <see cref="BeginContext"/>), any exception will be caught
        /// and routed to the handler instead of <see cref="WaitAllAsync"/>. If no handler has been registered, the
        /// exception will not be caught even though it will be recorded and thrown by <see cref="WaitAllAsync"/>.
        /// </para>
        /// </summary>
        public static void Post(Action? postCallbackAction)
        {
            // Install a default synchronization context if one does not exist
            Post(AsyncOperationManager.SynchronizationContext, postCallbackAction);
        }

        /// <summary>
        /// <para>
        /// Executes the specified delegate on the current <see cref="SynchronizationContext"/> while tracking so that
        /// any exception is handled and so that <see cref="WaitAllAsync"/> waits for its completion.
        /// </para>
        /// <para>
        /// <see cref="ArgumentNullException"/> is thrown if <paramref name="synchronizationContext"/> is <see
        /// langword="null"/>.
        /// </para>
        /// <para>
        /// If an exception handler has been registered (see <see cref="BeginContext"/>), any exception will be caught
        /// and routed to the handler instead of <see cref="WaitAllAsync"/>. If no handler has been registered, the
        /// exception will not be caught even though it will be recorded and thrown by <see cref="WaitAllAsync"/>.
        /// </para>
        /// </summary>
        public static void Post(SynchronizationContext synchronizationContext, Action? postCallbackAction)
        {
            if (synchronizationContext is null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            if (postCallbackAction is null) return;

            var context = CurrentContext;
            context.StartTask();

            var closure = new PostClosure<Action>(context, postCallbackAction);

            var postReturned = false;
            try
            {
                synchronizationContext.Post(OnPostAction, closure);
                postReturned = true;
            }
            finally
            {
                if (!postReturned && closure.TryClaimInvocation())
                {
                    context.EndTask();
                }
            }
        }

        private static void OnPostAction(object? state)
        {
            var closure = (PostClosure<Action>)state!;
            if (!closure.TryClaimInvocation()) return;
            try
            {
                closure.State.Invoke();
            }
            catch (Exception ex) when (closure.Context.RecordAndTrySuppress(new[] { ex }))
            {
            }
            finally
            {
                closure.Context.EndTask();
            }
        }

        /// <summary>
        /// <para>
        /// Executes the specified delegate on the current <see cref="SynchronizationContext"/> while tracking so that
        /// any exception is handled and so that <see cref="WaitAllAsync"/> waits for completion of the returned task.
        /// </para>
        /// <para>
        /// A default <see cref="SynchronizationContext"/> is installed if the current one is <see langword="null"/>.
        /// </para>
        /// <para>
        /// If an exception handler has been registered (see <see cref="BeginContext"/>), any exception will be caught
        /// and routed to the handler instead of <see cref="WaitAllAsync"/>. If no handler has been registered, the
        /// exception will not be caught even though it will be recorded and thrown by <see cref="WaitAllAsync"/>.
        /// </para>
        /// </summary>
        public static void Post(Func<Task>? postCallbackAsyncAction)
        {
            // Install a default synchronization context if one does not exist
            Post(AsyncOperationManager.SynchronizationContext, postCallbackAsyncAction);
        }

        /// <summary>
        /// <para>
        /// Executes the specified delegate on the current <see cref="SynchronizationContext"/> while tracking so that
        /// any exception is handled and so that <see cref="WaitAllAsync"/> waits for completion of the returned task.
        /// </para>
        /// <para>
        /// <see cref="ArgumentNullException"/> is thrown if <paramref name="synchronizationContext"/> is <see
        /// langword="null"/>.
        /// </para>
        /// <para>
        /// If an exception handler has been registered (see <see cref="BeginContext"/>), any exception will be caught
        /// and routed to the handler instead of <see cref="WaitAllAsync"/>. If no handler has been registered, the
        /// exception will not be caught even though it will be recorded and thrown by <see cref="WaitAllAsync"/>.
        /// </para>
        /// </summary>
        public static void Post(SynchronizationContext synchronizationContext, Func<Task>? postCallbackAsyncAction)
        {
            if (synchronizationContext is null)
                throw new ArgumentNullException(nameof(synchronizationContext));

            if (postCallbackAsyncAction is null) return;

            var context = CurrentContext;
            context.StartTask();

            var closure = new PostClosure<Func<Task>>(context, postCallbackAsyncAction);

            var postReturned = false;
            try
            {
                synchronizationContext.Post(OnPostAsyncAction, closure);
                postReturned = true;
            }
            finally
            {
                if (!postReturned && closure.TryClaimInvocation())
                {
                    context.EndTask();
                }
            }
        }

        private static void OnPostAsyncAction(object? state)
        {
            var closure = (PostClosure<Func<Task>>)state!;
            if (!closure.TryClaimInvocation()) return;

            try
            {
                Add(closure.State.Invoke());
            }
            catch (Exception ex) when (closure.Context.RecordAndTrySuppress(new[] { ex }))
            {
            }
            finally
            {
                closure.Context.EndTask();
            }
        }
    }
}
