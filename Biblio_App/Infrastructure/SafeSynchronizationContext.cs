using System;
using System.Threading;

namespace Biblio_App.Infrastructure
{
    /// <summary>
    /// Proxy SynchronizationContext that guarantees the posted/send delegate receives the intended state
    /// even if the inner context ignores or alters the state parameter.
    /// </summary>
    public sealed class SafeSynchronizationContext : SynchronizationContext
    {
        private readonly SynchronizationContext? _inner;

        public SafeSynchronizationContext(SynchronizationContext? inner) => _inner = inner;

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));

            if (_inner == null)
            {
                // Execute synchronously on current thread.
                d(state);
                return;
            }

            // Wrap the callback so the original delegate always receives 'state' via closure,
            // even if the inner context calls the callback with a different/NULL argument.
            _inner.Send(_ => d(state), null);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));

            if (_inner == null)
            {
                // No inner context — use thread pool to post asynchronously.
                ThreadPool.QueueUserWorkItem(_ => d(state));
                return;
            }

            // Post a wrapper that ignores the argument the inner context might pass and uses the captured state.
            _inner.Post(_ => d(state), null);
        }

        public override SynchronizationContext CreateCopy() => new SafeSynchronizationContext(_inner?.CreateCopy());

        public override void OperationStarted() => _inner?.OperationStarted();

        public override void OperationCompleted() => _inner?.OperationCompleted();
    }
}
