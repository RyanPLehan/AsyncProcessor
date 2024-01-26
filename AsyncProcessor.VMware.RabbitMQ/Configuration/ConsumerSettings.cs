using RabbitMQ.Client;
using System;

namespace AsyncProcessor.VMware.RabbitMQ.Configuration
{
    public class ConsumerSettings : ConnectionSettings
    {
        private const int PREFETCH_FACTOR = 5;
        private const int VALUE_NOT_SET = Int32.MinValue;
        private const int MIN_PREFETCH_COUNT = 0;
        private const int MIN_CONCURRENT_DISPATCH = 1;

        private int _prefetchCount = VALUE_NOT_SET;
        private int _concurrentDispatch = MIN_CONCURRENT_DISPATCH;

        /// <summary>
        /// Queue settings only if declaring a queue on the fly
        /// </summary>
        /// <remarks>
        /// Generally, queues should be created on the server with the appropriate settings.
        /// These settings will not override any existing queue
        /// </remarks>
        public QueueSettings Queue { get; set; } = null;


        public int PrefetchCount
        {
            // Use the set value.  If it has not been set, then calculate based upon concurrent dispatch value
            get { return this._prefetchCount == VALUE_NOT_SET ? this._concurrentDispatch * PREFETCH_FACTOR : this._prefetchCount; }

            // Ensure the value given is not smaller than the minimum value
            set { this._prefetchCount = Math.Max(MIN_PREFETCH_COUNT, value); }
        }

        public int ConcurrentDispatch
        {
            get { return this._concurrentDispatch; }

            // Ensure the value given is not smaller than the minimum value
            set { this._concurrentDispatch = Math.Max(MIN_CONCURRENT_DISPATCH, value); }
        }
    }
}
