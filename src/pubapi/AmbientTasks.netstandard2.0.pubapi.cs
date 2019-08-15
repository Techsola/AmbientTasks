// Name:       AmbientTasks
// Public key: ACQAAASAAACUAAAABgIAAAAkAABSU0ExAAQAAAEAAQARDo8hIGALQgYxKnlzGV87znoRWHmkZVTTVUa2zHFPI9LkTRj9+IWn+lb3zcLTJ/lWSz3LMa8BBMVZHNXNwC+z6WhpS/pKdBb2bFUVgROfZEcI0COMfCVHrAawYgkNHUjdlkhm+0Q5rymrJWKf2eLgBr8P2lLeXzAwd1Decd9Wxw==

namespace Techsola
{
    [System.Runtime.CompilerServices.NullableContext(1)]
    [System.Runtime.CompilerServices.Nullable(0)]
    public static class AmbientTasks
    {
        [System.Runtime.CompilerServices.NullableContext(2)]
        public static void Add(System.Threading.Tasks.Task task);

        public static void BeginContext([System.Runtime.CompilerServices.Nullable(new byte[] { 2, 1 })] System.Action<System.Exception> exceptionHandler = default);

        [System.Runtime.CompilerServices.NullableContext(2)]
        public static void Post(System.Threading.SendOrPostCallback d, object state);

        [System.Runtime.CompilerServices.NullableContext(2)]
        public static void Post([System.Runtime.CompilerServices.Nullable(1)] System.Threading.SynchronizationContext synchronizationContext, System.Threading.SendOrPostCallback d, object state);

        [System.Runtime.CompilerServices.NullableContext(2)]
        public static void Post(System.Action postCallbackAction);

        public static void Post(System.Threading.SynchronizationContext synchronizationContext, [System.Runtime.CompilerServices.Nullable(2)] System.Action postCallbackAction);

        public static System.Threading.Tasks.Task WaitAllAsync();
    }
}
