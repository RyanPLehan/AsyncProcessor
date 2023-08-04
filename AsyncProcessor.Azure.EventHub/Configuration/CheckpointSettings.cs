using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncProcessor.Azure.EventHub.Configuration
{
    public class CheckpointSettings
    {
        public int CheckpointIntervalInSeconds { get; set; } = 15;
        public string StorageConnectionString { get; set; }
        public string BlobContainerName { get; set; }
    }
}
