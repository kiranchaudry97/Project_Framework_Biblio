using System;
using System.Threading;

namespace Biblio_App.Infrastructure
{
    /// <summary>
    /// Proxy SynchronizationContext die garandeert dat de delegate
    /// altijd het correcte 'state'-object ontvangt.
    ///
    /// Dit is nodig omdat sommige SynchronizationContexts (bv. platform-
    /// of framework-implementaties) het state-object kunnen negeren,
    /// vervangen of null doorgeven.
    /// </summary>
    public sealed class SafeSynchronizationContext : SynchronizationContext
    {
        // De onderliggende (echte) SynchronizationContext
        private readonly SynchronizationContext? _inner;

        // Constructor: ontvangt de originele context (kan null zijn)
        public SafeSynchronizationContext(SynchronizationContext? inner)
        {
            _inner = inner;
        }

        // =====================================================
        // SEND
        // =====================================================
        // Voert een delegate synchroon uit op de doel-context
        public override void Send(SendOrPostCallback d, object? state)
        {
            // Validatie: delegate mag niet null zijn
            if (d == null)
                throw new ArgumentNullException(nameof(d));

            // Indien er geen onderliggende context is:
            // voer de delegate onmiddellijk uit op de huidige thread
            if (_inner == null)
            {
                d(state);
                return;
            }

            // Wrapper:
            // - Negeert het argument dat de inner context eventueel meegeeft
            // - Gebruikt altijd het originele 'state' via closure
            _inner.Send(_ => d(state), null);
        }

        // =====================================================
        // POST
        // =====================================================
        // Voert een delegate asynchroon uit
        public override void Post(SendOrPostCallback d, object? state)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));

            // Geen inner context?
            // ? gebruik de ThreadPool
            if (_inner == null)
            {
                ThreadPool.QueueUserWorkItem(_ => d(state));
                return;
            }

            // Post een wrapper delegate:
            // - Argument van inner context wordt genegeerd
            // - Originele 'state' wordt altijd gebruikt
            _inner.Post(_ => d(state), null);
        }

        // =====================================================
        // CREATE COPY
        // =====================================================
        // Maakt een veilige kopie van deze context
        public override SynchronizationContext CreateCopy()
        {
            return new SafeSynchronizationContext(_inner?.CreateCopy());
        }

        // =====================================================
        // OPERATION TRACKING
        // =====================================================
        // Forward operation tracking naar de inner context
        public override void OperationStarted()
        {
            _inner?.OperationStarted();
        }

        public override void OperationCompleted()
        {
            _inner?.OperationCompleted();
        }
    }
}
