using System;
using System.Threading.Tasks;

namespace VORP.Housing.Server.Scripts
{
    public class Manager : BaseScript
    {
        public PlayerList PlayerList => PluginManager.Instance.PlayerList;
        public ExportDictionary Export => PluginManager.Instance.ExportRegistry;
        public void AddEvent(string eventName, Delegate @delegate) => PluginManager.Instance.Hook(eventName, @delegate);
        public void AttachTickHandler(Func<Task> task) => PluginManager.Instance.AttachTickHandler(task);
        public void DetachTickHandler(Func<Task> task) => PluginManager.Instance.AttachTickHandler(task);
    }
}
