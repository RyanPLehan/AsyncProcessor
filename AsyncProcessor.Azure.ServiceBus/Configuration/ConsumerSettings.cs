using Azure.Messaging.ServiceBus;
using System;

namespace AsyncProcessor.Azure.ServiceBus.Configuration
{
    public class ConsumerSettings : ConnectionSettings
    {
        private const int PREFETCH_FACTOR = 5;
        private const int VALUE_NOT_SET = Int32.MinValue;
        private const int MIN_PREFETCH_COUNT = 0;
        private const int MIN_CONCURRENT_DISPATCH = 1;

        private int _prefetchCount = VALUE_NOT_SET;
        private int _concurrentDispatch = MIN_CONCURRENT_DISPATCH;

        public ServiceBusReceiveMode ReceiveMode { get; set; } = ServiceBusReceiveMode.PeekLock;

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
