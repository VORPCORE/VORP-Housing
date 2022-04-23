using System;
using System.Threading.Tasks;

namespace VORP.Housing.Shared
{
    public class BasePluginManager : BaseScript
    {
        public void AttachTickHandler(Func<Task> task)
        {
            Tick += task;
        }

        public void DetachTickHandler(Func<Task> task)
        {
            Tick -= task;
        }

        public void Hook(string eventName, Delegate @delegate)
        {
            EventHandlers[eventName] += @delegate;
        }
    }
}
