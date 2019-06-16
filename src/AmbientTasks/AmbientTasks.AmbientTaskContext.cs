using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Techsola
{
    partial class AmbientTasks
    {
        private sealed class AmbientTaskContext
        {
            private readonly Action<Exception>? exceptionHandler;

            /// <summary>
            /// Doubles as a lockable object for all access to <see cref="bufferedExceptions"/>, <see cref="currentTaskCount"/>,
            /// and <see cref="waitAllSource"/>.
            /// </summary>
            private readonly List<Exception> bufferedExceptions = new List<Exception>();
            private int currentTaskCount;
            private TaskCompletionSource<object?>? waitAllSource;

            public AmbientTaskContext(Action<Exception>? exceptionHandler)
            {
                this.exceptionHandler = exceptionHandler;
            }

            [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "All exceptions are caught and passed to the appropriate handler by design.")]
            public bool RecordAndTrySuppress(IReadOnlyCollection<Exception> exceptions)
            {
                if (exceptionHandler is null)
                {
                    lock (bufferedExceptions)
                    {
                        // Don't wrap yet. WaitAllAsync will wrap in AggregateException after collecting unhandled exceptions.
                        bufferedExceptions.AddRange(exceptions);
                    }

                    return false;
                }

                try
                {
                    // The top-level handler should not normally care about the exception type, so conditionally
                    // wrapping should not be a pit of failure like it is in situations where you need to catch specific
                    // exceptions.
                    exceptionHandler.Invoke(exceptions.Count == 1
                        ? exceptions.Single()
                        : new AggregateException(exceptions));
                }
                catch (Exception handlerException)
                {
                    lock (bufferedExceptions)
                    {
                        // Don't wrap yet. WaitAllAsync will wrap in AggregateException after collecting unhandled exceptions.
                        bufferedExceptions.AddRange(exceptions);
                    }

                    try
                    {
                        exceptionHandler.Invoke(handlerException);
                    }
                    catch (Exception secondHandlerException)
                    {
                        lock (bufferedExceptions)
                        {
                            bufferedExceptions.Add(handlerException);
                            bufferedExceptions.Add(secondHandlerException);
                        }
                    }
                }

                return true;
            }

            public void StartTask()
            {
                lock (bufferedExceptions)
                {
                    currentTaskCount = checked(currentTaskCount + 1);
                }
            }

            public void EndTask()
            {
                TaskCompletionSource<object?>? sourceToComplete;
                Exception[] endingExceptions;

                lock (bufferedExceptions)
                {
                    var newCount = currentTaskCount - 1;
                    if (newCount < 0) throw new InvalidOperationException($"More calls to {nameof(EndTask)} than {nameof(StartTask)}.");
                    currentTaskCount = newCount;
                    if (newCount > 0) return;

                    sourceToComplete = waitAllSource;
                    if (sourceToComplete is null) return; // No one is waiting
                    waitAllSource = null;

                    endingExceptions = bufferedExceptions.ToArray();
                    bufferedExceptions.Clear();
                }

                // Do not set the source inside the lock. Arbitrary user continuations may have been set on
                // sourceToComplete.Task since it was previously returned from WaitAllAsync, and executing arbitrary
                // user code within a lock is a very bad idea.
                if (endingExceptions.Any())
                    sourceToComplete.SetException(new AggregateException(endingExceptions));
                else
                    sourceToComplete.SetResult(null);
            }

            public Task WaitAllAsync()
            {
                lock (bufferedExceptions)
                {
                    if (waitAllSource != null) return waitAllSource.Task;

                    if (currentTaskCount > 0)
                    {
                        waitAllSource = new TaskCompletionSource<object?>();
                        return waitAllSource.Task;
                    }

                    if (bufferedExceptions.Any())
                    {
                        var source = new TaskCompletionSource<object?>();
                        source.SetException(new AggregateException(bufferedExceptions));
                        bufferedExceptions.Clear();
                        return source.Task;
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
}
