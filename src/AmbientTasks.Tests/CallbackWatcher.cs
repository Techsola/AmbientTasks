using System;
using NUnit.Framework;

namespace Techsola
{
    /// <summary>
    /// Provides idiomatic callback-based assertions.
    /// </summary>
    /// <example>
    /// <code>
    /// var watcher = new CallbackWatcher();
    ///
    /// systemUnderTest.PropertyChanged += (sender, e) => watcher.OnCallback();
    ///
    /// using (watcher.ExpectCallback())
    /// {
    ///     systemUnderTest.SomeProperty = 42;
    /// } // Fails if PropertyChanged did not fire
    ///
    /// systemUnderTest.SomeProperty = 42; // Fails if PropertyChanged fires
    /// </code>
    /// </example>
    public sealed class CallbackWatcher
    {
        private int expectedCount;
        private int actualCount;

        /// <summary>
        /// Begins expecting callbacks. When the returned scope is disposed, stops expecting callbacks and throws <see cref="AssertionException"/>
        /// if <see cref="OnCallback"/> was not called the expected number of times between beginning and ending.
        /// </summary>
        /// <param name="count">
        /// The number of times that <see cref="OnCallback"/> is expected to be called before the returned scope is disposed.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the returned scope from the previous call has not been disposed.</exception>
        /// <exception cref="AssertionException">Thrown when <see cref="OnCallback"/> was not called the expected number of times between beginning and ending.</exception>
        public IDisposable ExpectCallback(int count = 1)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), expectedCount, "Expected callback count must be greater than or equal to one.");

            if (expectedCount != 0)
                throw new InvalidOperationException($"The previous {nameof(ExpectCallback)} scope must be disposed before calling again.");

            expectedCount = count;
            actualCount = 0;

            return On.Dispose(() =>
            {
                try
                {
                    if (actualCount < expectedCount)
                        Assert.Fail($"Expected {(expectedCount == 1 ? "a single call" : expectedCount + " calls")}, but there {(actualCount == 1 ? "was" : "were")} {actualCount}.");
                }
                finally
                {
                    expectedCount = 0;
                }
            });
        }

        /// <summary>
        /// Call this from the callback being tested.
        /// Throws <see cref="AssertionException"/> if this watcher is not currently expecting a callback
        /// (see <see cref="ExpectCallback"/>) or if the expected number of callbacks has been exceeded.
        /// </summary>
        /// <exception cref="AssertionException">
        /// Thrown when this watcher is not currently expecting a callback (see <see cref="ExpectCallback"/>)
        /// or if the expected number of callbacks has been exceeded.
        /// </exception>
        public void OnCallback() => OnCallback(out _);

        /// <summary>
        /// Call this from the callback being tested.
        /// Throws <see cref="AssertionException"/> if this watcher is not currently expecting a callback
        /// (see <see cref="ExpectCallback"/>) or if the expected number of callbacks has been exceeded.
        /// </summary>
        /// <exception cref="AssertionException">
        /// Thrown when this watcher is not currently expecting a callback (see <see cref="ExpectCallback"/>)
        /// or if the expected number of callbacks has been exceeded.
        /// </exception>
        public void OnCallback(out int callCount)
        {
            actualCount++;
            callCount = actualCount;

            if (actualCount > expectedCount)
            {
                Assert.Fail(expectedCount == 0
                    ? "Expected no callback."
                    : $"Expected {(expectedCount == 1 ? "a single call" : expectedCount + " calls")}, but there were more.");
            }
        }
    }
}
