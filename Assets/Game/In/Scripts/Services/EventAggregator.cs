using System;

public static class EventAggregator
{
    public static void Subscribe<T>(Action<object, T> eventHandler)
    {
        Event<T>.EventHandler += eventHandler;
    }
    
    public static void Unsubscribe<T>(Action<object, T> eventHandler)
    {
        Event<T>.EventHandler -= eventHandler;
    }
    
    public static void Post<T>(object sender, T eventData)
    {
        Event<T>.Post(sender, eventData);
    }

    private static class Event<T>
    {
        public static event Action<object, T> EventHandler;

        public static void Post(object sender, T eventData)
        {
            EventHandler?.Invoke(sender, eventData);
        }
        
        public static void Clear()
        {
            EventHandler = null;
        }
    }
}