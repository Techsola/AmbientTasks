using System.Threading;

namespace Techsola
{
    public static partial class AmbientTasks
    {
        private sealed class PostClosure<TState>
        {
            private int wasInvoked;

            public AmbientTaskContext Context { get; }
            public TState State { get; }

            public PostClosure(AmbientTaskContext context, TState state)
            {
                Context = context;
                State = state;
            }

            public bool TryClaimInvocation() => Interlocked.Exchange(ref wasInvoked, 1) == 0;
        }
    }
}
