using System.Collections.Generic;
using System;
using UnityEngine;
using Scaffold.Events.Contracts;

namespace Scaffold.Events
{
    public class EventController : IEventBus
    {
        readonly Dictionary<Type, Action<ContextEvent>> events = new Dictionary<Type, Action<ContextEvent>>();
        readonly Dictionary<Delegate, Action<ContextEvent>> eventLookups = new Dictionary<Delegate, Action<ContextEvent>>();

        public void AddListener<T>(Action<T> evt) where T : ContextEvent
        {
            if (eventLookups.ContainsKey(evt)) { return; }
            Action<ContextEvent> newAction = CreateTypedAction(evt);
            eventLookups[evt] = newAction;
            AddListener(typeof(T), newAction);
        }

        public void AddListener(Type type, Action<ContextEvent> newAction)
        {
            ValidateListenerInput(type, newAction);
            events[type] = events.TryGetValue(type, out Action<ContextEvent> current) ? current + newAction : newAction;
        }

        private Action<ContextEvent> CreateTypedAction<T>(Action<T> evt) where T : ContextEvent
        {
            return e => evt((T)e);
        }

        public void RemoveListener<T>(Action<T> evt) where T : ContextEvent
        {
            if (!eventLookups.TryGetValue(evt, out var action))
            {
                return;
            }

            RemoveListener(typeof(T), action);
            eventLookups.Remove(evt);
        }

        public void RemoveListener(Type type, Action<ContextEvent> action)
        {
            if (!events.TryGetValue(type, out var tempAction))
            {
                return;
            }

            UpdateOrRemoveAction(type, tempAction, action);
        }

        private void UpdateOrRemoveAction(Type type, Action<ContextEvent> tempAction, Action<ContextEvent> action)
        {
            tempAction -= action;
            ApplyOrRemoveAction(type, tempAction);
        }

        private void ApplyOrRemoveAction(Type type, Action<ContextEvent> tempAction)
        {
            if (tempAction == null)
            {
                events.Remove(type);
            }
            else
            {
                events[type] = tempAction;
            }
        }

        public void Raise(ContextEvent evt)
        {
            ValidateEvent(evt);
            var evtType = evt.GetType();
            if (events.TryGetValue(evtType, out var action))
            {
                action.Invoke(evt);
            }
        }

        public void Clear()
        {
            ValidateState();
            events.Clear();
            eventLookups.Clear();
        }

        private void ValidateListenerInput(Type type, Action<ContextEvent> newAction)
        {
            ValidateState();
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (newAction == null) throw new ArgumentNullException(nameof(newAction));
        }

        private void ValidateEvent(ContextEvent evt)
        {
            ValidateState();
            if (evt == null) throw new ArgumentNullException(nameof(evt));
        }

        private void ValidateState()
        {
            if (events == null) throw new InvalidOperationException("Event registry was not initialized.");
            if (eventLookups == null) throw new InvalidOperationException("Event lookup registry was not initialized.");
        }
    }
}


