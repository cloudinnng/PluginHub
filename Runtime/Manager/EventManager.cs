using System;
using System.Collections.Generic;

/// <summary>
/// 可能会变的，多个监听者的，一对多的情况， 用事件做。比如有一个主角掉血事件。
/// 一般会在掉血位置调用HUD上血条的ui变化和主角头上的血条ui变化的代码
/// 如果还有别的地方也对主角血条变化事件感兴趣，那么又得在扣血位置添加新的一段处理代码。
/// 如果用EventManager,掉血的位置作为事件抛出血条变化的事件，并且在参数里传入变化后的血亮，
/// 那么别处对血条变化感兴趣的程序只需在对应位置注册对该事件的监听即可。
/// 这也是设计模式里的"观察者模式"
/// 注意：不用的时候要及时清除监听
/// </summary>
public class EventManager : SingletonMonoBehaviour<EventManager>
{
    //保存所有的处理者者
    public List<Handler> HandlerList = new List<Handler>();

    public void AddListener(string eventId, Action<object> action)
    {
        HandlerList.Add(new Handler(eventId, action));
    }
    public void RemoveListener(string eventId, Action<object> action)
    {
        HandlerList.Remove(new Handler(eventId, action));
    }
    public void ClearListener()
    {
        HandlerList.Clear();
    }
    public void Fire(string eventId, object data = null)
    {
        foreach (Handler aListener in HandlerList)
        {
            if (aListener.EventIdentifier.Equals(eventId))
            {
                aListener.HandlerAction(data);
            }
        }
    }
}
[Serializable]
public class Handler : IEquatable<Handler>
{
    /// <summary>
    /// 事件的识别码 如：playerHPChangeEvent
    /// </summary>
    public string EventIdentifier;

    /// <summary>
    /// 处理者在事件触发时的动作
    /// </summary>
    public Action<object> HandlerAction;

    public Handler(string eventIdentifier, Action<object> action)
    {
        this.EventIdentifier = eventIdentifier.Trim();
        this.HandlerAction = action;
    }

    public bool Equals(Handler other)
    {
        if (other == null) return false;

        return EventIdentifier == other.EventIdentifier && HandlerAction == other.HandlerAction;
    }
}
