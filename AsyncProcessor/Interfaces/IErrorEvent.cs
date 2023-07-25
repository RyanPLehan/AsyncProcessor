using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncProcessor
{
    public interface IErrorEvent
    {
        object EventData { get; }
        Exception Exception { get; }
        string Partition { get; }
    }
}
