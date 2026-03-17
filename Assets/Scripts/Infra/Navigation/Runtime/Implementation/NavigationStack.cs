using UnityEngine;
using Scaffold.Types;
using Scaffold.Events.Contracts;
using Scaffold.Events;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Scaffold.Navigation.Contracts;
namespace Scaffold.Navigation
{
    internal class NavigationStack
    {
        public int Count => stack.Count;
        public IView CurrentView => CurrentPoint?.View;
        public NavigationPoint CurrentPoint { get; private set; }
        public NavigationPoint PreviousPoint => Count <= 1 ? null : stack[^2];
        private List<NavigationPoint> stack = new List<NavigationPoint>();

        public NavigationPoint Get<T>()
        {
            GuardStackState();
            return Get(typeof(T));
        }

        public NavigationPoint Get(Type screenType)
        {
            GuardStackState();
            return stack.LastOrDefault(point => MatchesType(point, screenType));
        }

        private bool MatchesType(NavigationPoint point, Type screenType)
        {
            var viewType = point.View.GetType();
            var vmType = point.ViewModel.GetType();
            return screenType.IsAssignableFrom(viewType) || screenType.IsAssignableFrom(vmType);
        }

        public NavigationPoint Get(IView screen)
        {
            GuardStackState();
            return stack.LastOrDefault(point => point.View == screen);
        }

        public NavigationPoint Get(IViewController controller)
        {
            GuardStackState();
            return stack.LastOrDefault(point => point.ViewModel == controller);
        }

        public List<IView> GetAllScreens(Func<NavigationPoint, bool> filter)
        {
            GuardStackState();
            return GetAllStackedScreens(filter).Select(s => s.View).ToList();
        }

        public List<NavigationPoint> GetAllStackedScreens(Func<NavigationPoint, bool> filter = null)
        {
            filter ??= (s) => true;
            return stack.Where(s => filter.Invoke(s)).ToList();
        }

        public void AddToStack(NavigationPoint point)
        {
            GuardStackState();
            if (point != null)
            {
                stack.Add(point);
                CurrentPoint = point;
            }
        }

        public void RemoveFromStack(NavigationPoint point)
        {
            GuardStackState();
            if (point == null) { return; }
            stack.Remove(point);
            UpdateCurrentPointAfterRemoval(point);
        }

        private void UpdateCurrentPointAfterRemoval(NavigationPoint point)
        {
            if (CurrentPoint == point) { CurrentPoint = stack.LastOrDefault(); }
        }

        public int GetPointDepth(NavigationPoint point)
        {
            GuardStackState();
            int index = stack.IndexOf(point);
            if (index != 0)
                return Mathf.Max(index * 10, stack[index - 1].Depth + 10);
            else
            {
                return 0;
            }
        }

        public void ClearStack()
        {
            GuardStackState();
            stack.Clear();
        }

        private void GuardStackState()
        {
            if (stack == null) { throw new InvalidOperationException("Navigation stack is not initialized."); }
        }
    }

}




