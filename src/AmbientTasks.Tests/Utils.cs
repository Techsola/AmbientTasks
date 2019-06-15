using System;
using System.Threading;

namespace Techsola
{
    internal static class Utils
    {
        public static IDisposable WithTemporarySynchronizationContext(SynchronizationContext context)
        {
            var originalSynchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);

            return On.Dispose(() =>
            {
                if (SynchronizationContext.Current == context)
                    SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
            });
        }
    }
}
