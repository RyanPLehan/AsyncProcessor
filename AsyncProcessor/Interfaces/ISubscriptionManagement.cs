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
        Task Attach(string topic);

        /// <summary>
        /// Attach to Topic and Subscription
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        Task Attach(string topic, string subscription);

        /// <summary>
        /// Detach from Topic or Queue
        /// </summary>
        /// <returns></returns>
        Task Detach();

        /// <summary>
        /// Pause from receiving messages without fully detaching
        /// </summary>
        /// <returns></returns>
        Task Pause();

        /// <summary>
        /// Resume receiving messages without fully attaching
        /// </summary>
        /// <returns></returns>
        Task Resume();
    }
}
