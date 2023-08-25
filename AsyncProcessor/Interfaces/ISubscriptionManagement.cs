using System;
using System.Threading.Tasks;

namespace AsyncProcessor
{
    public interface ISubscriptionManagement
    {
        /// <summary>
        /// Attach to Topic or Queue
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        Task Attach(string topic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attach to Topic and Subscription
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        Task Attach(string topic, string subscription, CancellationToken cancellationToken = default);

        /// <summary>
        /// Detach from Topic or Queue
        /// </summary>
        /// <returns></returns>
        Task Detach(CancellationToken cancellationToken = default);

        /// <summary>
        /// Pause from receiving messages without fully detaching
        /// </summary>
        /// <returns></returns>
        Task Pause(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resume receiving messages without fully attaching
        /// </summary>
        /// <returns></returns>
        Task Resume(CancellationToken cancellationToken = default);
    }
}
