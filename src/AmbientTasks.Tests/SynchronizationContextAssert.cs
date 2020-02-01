using System;
using System.Threading;
using NUnit.Framework;

namespace Techsola
{
    public static class SynchronizationContextAssert
    {
        public static IDisposable ExpectNoPost()
        {
            var context = new MockSynchronizationContext(testPostedAction: null);

            return Utils.WithTemporarySynchronizationContext(context);
        }

        public static IDisposable ExpectSinglePost(Action<Action> testPostedAction)
        {
            if (testPostedAction is null) throw new ArgumentNullException(nameof(testPostedAction));

            var context = new MockSynchronizationContext(testPostedAction);
            var withTempContext = Utils.WithTemporarySynchronizationContext(context);

            return On.Dispose(() =>
            {
                withTempContext.Dispose();
                if (!context.ReceivedPost) Assert.Fail("Expected a call to SynchronizationContext.Post.");
            });
        }

        private sealed class MockSynchronizationContext : SynchronizationContext
        {
            private readonly Action<Action>? testPostedAction;

            public MockSynchronizationContext(Action<Action>? testPostedAction)
            {
                this.testPostedAction = testPostedAction;
            }

            public bool ReceivedPost { get; private set; }

            public override void Send(SendOrPostCallback d, object? state)
            {
                Assert.Fail("Expected no call to SynchronizationContext.Send.");
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                if (testPostedAction is null) Assert.Fail("Expected no calls to SynchronizationContext.Post.");
                if (ReceivedPost) Assert.Fail("Expected no more than one call to SynchronizationContext.Post.");
                ReceivedPost = true;

                testPostedAction!.Invoke(() => d.Invoke(state));
            }
        }
    }
}
