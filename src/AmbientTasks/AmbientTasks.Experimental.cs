using System;
using System.Threading;

namespace Techsola
{
    partial class AmbientTasks
    {
        /// <summary>
        /// ⚠ These APIs may be removed without notice in any future version. Use at your own risk.
        /// </summary>
        public static class Experimental
        {
            /// <summary>
            /// <para>
            /// <see cref="BeginContext"/> should be used instead. Use of this method should be considered a code smell.
            /// </para>
            /// <para>
            /// If <see cref="BeginContext"/> handler is not receiving what you expected, it is because the execution
            /// context is not flowing to where <c>AmbientTasks.Add</c> or <c>Post</c> is being called. The lack of
            /// execution context flow is a problem that should be fixed rather than relying on a shared global context.
            /// As a precaution, <see cref="WaitAllAsync"/> throws if there is no flowed context and there is a global
            /// context.
            /// </para>
            /// <para>
            /// <b>⚠ This API may be removed without notice in any future version. Use at your own risk.</b> If you find
            /// this API essential, please comment on <a
            /// href="https://github.com/Techsola/AmbientTasks/issues/4">https://github.com/Techsola/AmbientTasks/issues/4</a>.
            /// </para>
            /// </summary>
            public static void EnableGlobalFallbackContext(Action<Exception>? exceptionHandler)
            {
                if (Interlocked.CompareExchange(ref experimentalFallbackContext, new AmbientTaskContext(exceptionHandler), null) != null)
                {
                    throw new InvalidOperationException("The global fallback handler has already been set.");
                }
            }
        }
    }
}
