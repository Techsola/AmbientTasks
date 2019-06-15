using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Techsola
{
    partial class AmbientTasks
    {
        private sealed class AmbientTaskContext
        {
            private readonly Action<Exception> exceptionHandler;

            /// <summary>
            /// Doubles as a lockable object for all access to <see cref="exceptions"/>, <see cref="currentTaskCount"/>,
            /// and <see cref="waitAllSource"/>.
            /// </summary>
            private readonly List<Exception> exceptions = new List<Exception>();
            private int currentTaskCount;
            private TaskCompletionSource<object> waitAllSource;

            public AmbientTaskContext(Action<Exception> exceptionHandler)
            {
                this.exceptionHandler = exceptionHandler;
            }

            public bool RecordAndTrySuppress(Exception exception)
            {
                if (exceptionHandler is null)
                {
                    lock (exceptions)
                        exceptions.Add(exception);
                    return false;
                }

                try
                {
                    exceptionHandler.Invoke(exception);
                }
                catch (Exception handlerException)
                {
                    lock (exceptions)
                        exceptions.Add(exception);

                    try
                    {
                        exceptionHandler.Invoke(handlerException);
                    }
                    catch (Exception secondHandlerException)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(handlerException);
                            exceptions.Add(secondHandlerException);
                        }
                    }
                }

                return true;
            }

            public void StartTask()
            {
                lock (exceptions)
                {
                    currentTaskCount = checked(currentTaskCount + 1);
                }
            }

            public void EndTask()
            {
                TaskCompletionSource<object> sourceToComplete;
                Exception[] bufferedExceptions;

                lock (exceptions)
                {
                    var newCount = currentTaskCount - 1;
                    if (newCount < 0) throw new InvalidOperationException($"More calls to {nameof(EndTask)} than {nameof(StartTask)}.");
                    currentTaskCount = newCount;
                    if (newCount > 0) return;

                    sourceToComplete = waitAllSource;
                    if (sourceToComplete is null) return; // No one is waiting
                    waitAllSource = null;

                    bufferedExceptions = exceptions.ToArray();
                    exceptions.Clear();
                }

                // Do not set the source inside the lock. Arbitrary user continuations may have been set on
                // sourceToComplete.Task since it was previously returned from WaitAllAsync, and executing arbitrary
                // user code within a lock is a very bad idea.
                if (bufferedExceptions.Any())
                    sourceToComplete.SetException(bufferedExceptions);
                else
                    sourceToComplete.SetResult(null);
            }

            public Task WaitAllAsync()
            {
                lock (exceptions)
                {
                    if (waitAllSource != null) return waitAllSource.Task;

                    if (currentTaskCount > 0)
                    {
                        waitAllSource = new TaskCompletionSource<object>();
                        return waitAllSource.Task;
                    }

                    if (exceptions.Any())
                    {
                        var source = new TaskCompletionSource<object>();
                        source.SetException(exceptions);
                        exceptions.Clear();
                        return source.Task;
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
}
