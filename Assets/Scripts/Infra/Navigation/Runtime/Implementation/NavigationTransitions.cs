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
    internal class NavigationTransitions
    {
        public NavigationTransitions(IEventBus events)
        {
            if (events is null) { throw new ArgumentNullException(nameof(events)); }
            this.events = events;
        }

        private IEventBus events;
        private bool runningTransition = false;
        private Queue<ViewTransitionData> pendingTransitions = new Queue<ViewTransitionData>();

        public Action<ViewTransitionData> TransitionFinished = delegate { };

        public void DoTransition(NavigationPoint from, NavigationPoint to, bool closeCurrent)
        {
            GuardTransitionRequest(from, to);
            var transitionData = new ViewTransitionData(from, to, closeCurrent);
            pendingTransitions.Enqueue(transitionData);
            if (!runningTransition)
            {
                RunTransitions();
            }
        }

        private async void RunTransitions()
        {
            runningTransition = true;
            while (pendingTransitions.Count > 0)
            {
                await ProcessNextTransition();
            }
            runningTransition = false;
        }

        private async Task ProcessNextTransition()
        {
            var transition = pendingTransitions.Dequeue();
            if (transition != null)
            {
                await EnsureTargetPointReady(transition);
                SetupTransitionSequences(transition);
                await ExecuteTransition(transition);
                TransitionFinished?.Invoke(transition);
            }
        }

        private async Task EnsureTargetPointReady(ViewTransitionData transition)
        {
            if (transition?.To == null) { return; }
            await transition.To.EnsureReadyAsync();
        }

        private void GuardTransitionRequest(NavigationPoint from, NavigationPoint to)
        {
            if (from == null && to == null) { throw new ArgumentException("At least one navigation point is required."); }
            if (pendingTransitions == null) { throw new InvalidOperationException("Pending transition queue is not initialized."); }
        }

        private void SetupTransitionSequences(ViewTransitionData transition)
        {
            transition.OpenningSequence = () => DoOpenSequence(transition.From, transition.To);
            transition.ClosingSequence = () => DoCloseSequence(transition.From, transition.To);
            transition.HidingSequence = () => DoHideSequence(transition.From, transition.To);
        }

        private async Task ExecuteTransition(ViewTransitionData transition)
        {
            var transitionSchema = GetTransitionSchema(transition, out var direction);
            if (transitionSchema != null)
            {
                await ResolveTransitionSchema(transitionSchema, transition, direction);
                return;
            }
            await DefaultViewTransition(transition);
        }

        private TransitionViewSchema GetTransitionSchema(ViewTransitionData transition, out TransitionDirection direction)
        {
            if (TryGetFromSchema(transition, out var fromSchema))
            {
                direction = TransitionDirection.FromThisView;
                return fromSchema;
            }
            return GetToTransitionSchema(transition, out direction);
        }

        private TransitionViewSchema GetToTransitionSchema(ViewTransitionData transition, out TransitionDirection direction)
        {
            if (TryGetToSchema(transition, out var toSchema))
            {
                direction = TransitionDirection.ToThisView;
                return toSchema;
            }
            direction = default;
            return null;
        }

        private bool TryGetFromSchema(ViewTransitionData transition, out TransitionViewSchema schema)
        {
            schema = null;
            if (transition?.From?.Config == null) { return false; }
            if (!transition.From.Config.TryGetSchema<TransitionViewSchema>(out schema)) { return false; }
            return schema.IsValidTransition(transition.From, transition.To, TransitionDirection.FromThisView);
        }

        private bool TryGetToSchema(ViewTransitionData transition, out TransitionViewSchema schema)
        {
            schema = null;
            if (transition?.From?.Config == null) { return false; }
            if (!transition.To.Config.TryGetSchema<TransitionViewSchema>(out schema)) { return false; }
            return schema.IsValidTransition(transition.From, transition.To, TransitionDirection.ToThisView);
        }

        private async Task ResolveTransitionSchema(TransitionViewSchema schema, ViewTransitionData transition, TransitionDirection direction)
        {
            if (schema.Handler is TransitionHandler.Code)
            {
                await HandleCodeTransitions(transition, direction);
            }
            else
            {
                await HandleNonCodeTransition(schema, transition);
            }
        }

        private async Task HandleCodeTransitions(ViewTransitionData transition, TransitionDirection direction)
        {
            var point = direction is TransitionDirection.ToThisView ? transition.To : transition.From;
            await point.View.gameObject.GetComponent<IViewTransitionHandler>().DoTransition(transition, direction);
        }

        private async Task HandleNonCodeTransition(TransitionViewSchema schema, ViewTransitionData transition)
        {
            if (schema.Handler is TransitionHandler.Template)
            {
                throw new Exception("No handler for template transitions was defined yet");
            }
            if (schema.Handler is TransitionHandler.Default)
            {
                await DefaultViewTransition(transition);
            }
        }

        private async Task DefaultViewTransition(ViewTransitionData transition)
        {
            if (transition.From != null)
            {
                await DefaultFromTransition(transition);
            }
            if (transition.To != null)
            {
                await transition.OpenningSequence();
            }
        }

        private async Task DefaultFromTransition(ViewTransitionData transition)
        {
            if (transition.CloseCurrent)
            {
                await transition.ClosingSequence();
            }
            else
            {
                await transition.HidingSequence();
            }
        }

        private async Task DoCloseSequence(NavigationPoint from, NavigationPoint to)
        {
            var viewType = from.ViewModel.GetType();
            var beforeCloseEvent = new BeforeViewCloseEvent(viewType);
            events.Raise(beforeCloseEvent);
            await RunAnimationIfPresent(from, to, AnimationType.Closing, from);
            this.Close(from);
            var afterCloseEvent = new AfterViewCloseEvent(viewType);
            events.Raise(afterCloseEvent);
        }

        private async Task DoHideSequence(NavigationPoint from, NavigationPoint to)
        {
            var viewType = from.ViewModel.GetType();
            var beforeHideEvent = new BeforeViewCloseEvent(viewType);
            events.Raise(beforeHideEvent);
            await RunAnimationIfPresent(from, to, AnimationType.Hiding, from);
            this.Hide(from, to);
            var afterHideEvent = new AfterViewCloseEvent(viewType);
            events.Raise(afterHideEvent);
        }

        private async Task DoOpenSequence(NavigationPoint from, NavigationPoint to)
        {
            var viewType = to.ViewModel.GetType();
            var beforeOpenEvent = new BeforeViewOpenEvent(viewType);
            events.Raise(beforeOpenEvent);
            await ActivateAndOpen(from, to);
            var afterOpenEvent = new AfterViewOpenEvent(viewType);
            events.Raise(afterOpenEvent);
        }

        private async Task ActivateAndOpen(NavigationPoint from, NavigationPoint to)
        {
            to.View.gameObject.SetActive(true);
            if (to.View.State is ViewState.Closed)
            {
                to.View.Bind(to.ViewModel);
            }
            await RunAnimationIfPresent(from, to, AnimationType.Opening, to);
            this.Open(to);
        }

        private async Task RunAnimationIfPresent(NavigationPoint from, NavigationPoint to, AnimationType animType, NavigationPoint target)
        {
            var schema = GetAnimationSchema(from, to, animType);
            if (schema != null)
            {
                await ResolveAnimationSchema(schema, target, animType);
            }
        }

        private void Close(NavigationPoint point)
        {
            if (point.View == null || point.View.gameObject == null)
            {
                return;
            }
            point.View.Close();
            point.Dispose();
        }

        private void Hide(NavigationPoint point, NavigationPoint to)
        {
            if (point.View == null || point.View.gameObject == null)
            {
                return;
            }
            HideIfApplicable(point, to);
        }

        private void HideIfApplicable(NavigationPoint point, NavigationPoint to)
        {
            bool toHasNoView = to.View == null || to.View.gameObject == null;
            bool toIsScreen = !toHasNoView && to.View.Type is ViewType.Screen;
            if (toHasNoView || toIsScreen)
            {
                point.View.Hide();
            }
        }

        private void Open(NavigationPoint point)
        {
            try
            {
                TryOpenPoint(point);
            }
            catch (Exception ex)
            {
                LogOpenError(ex);
            }
        }

        private void TryOpenPoint(NavigationPoint point)
        {
            if (point?.Disposed == true || point?.View == null || point?.View?.gameObject == null)
            {
                return;
            }
            FocusOrOpenView(point);
        }

        private void FocusOrOpenView(NavigationPoint point)
        {
            if (point.View.State is ViewState.Open)
            {
                point.View.Focus();
            }
            else
            {
                point.View.Open();
            }
        }

        private void LogOpenError(Exception ex)
        {
            Debug.Log("Catched a problem while opening a point");
            Debug.LogException(ex);
        }

        private AnimationViewSchema GetAnimationSchema(NavigationPoint from, NavigationPoint to, AnimationType direction)
        {
            var config = direction is AnimationType.Opening ? to.Config : from.Config;
            var schemas = config.GetSchemas<AnimationViewSchema>();
            return schemas.FirstOrDefault(s => s.IsValidAnimation(from, to, direction));
        }

        private async Task ResolveAnimationSchema(AnimationViewSchema animationSchema, NavigationPoint point, AnimationType direction)
        {
            if (animationSchema.Handler is AnimationHandler.Animator)
            {
                await this.HandleAnimator(animationSchema, point);
            }
            else
            {
                await HandleNonAnimatorAnimation(animationSchema, point, direction);
            }
        }

        private async Task HandleAnimator(AnimationViewSchema schema, NavigationPoint point)
        {
            await WaitForSecondsAsync(0.02f);
            var animator = point.View.gameObject.GetComponent<Animator>();
            animator.Play(schema.AnimationName);
            await WaitForAnimationState(animator, schema.AnimationName);
        }

        private async Task WaitForAnimationState(Animator animator, string animationName)
        {
            var stateHash = Animator.StringToHash(animationName);
            if (!animator.HasState(0, stateHash))
            {
                throw new System.Exception($"No valid state {animationName} in this animator");
            }
            await WaitForAnimationCompletion(animator, animationName);
        }

        private async Task WaitForAnimationCompletion(Animator animator, string animationName)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            while (!state.IsName(animationName))
            {
                await WaitForSecondsAsync(0.1f);
                state = animator.GetCurrentAnimatorStateInfo(0);
            }
            await WaitForAnimationEnd(animator, animationName);
        }

        private async Task WaitForAnimationEnd(Animator animator, string animationName)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            while (state.IsName(animationName) && state.normalizedTime <= 1)
            {
                await WaitForSecondsAsync(0.1f);
                state = animator.GetCurrentAnimatorStateInfo(0);
            }
            await WaitForNormalizedTimeEnd(animator);
        }

        private async Task WaitForNormalizedTimeEnd(Animator animator)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            while (state.normalizedTime <= 1)
            {
                await WaitForSecondsAsync(0.1f);
                state = animator.GetCurrentAnimatorStateInfo(0);
            }
        }

        private async Task HandleNonAnimatorAnimation(AnimationViewSchema animationSchema, NavigationPoint point, AnimationType direction)
        {
            if (animationSchema.Handler is AnimationHandler.Code)
            {
                await this.HandleCodeAnimation(point, direction);
            }
            else if (animationSchema.Handler is AnimationHandler.Template)
            {
                throw new System.Exception("No handler for template animations was defined yet");
            }
        }

        private async Task HandleCodeAnimation(NavigationPoint point, AnimationType direction)
        {
            await point.View.gameObject.GetComponent<IViewAnimationHandler>().AnimateView(direction);
        }

        private Task WaitForSecondsAsync(float seconds)
        {
            var delayFromSeconds = Mathf.CeilToInt(seconds * 1000f);
            var milliseconds = Mathf.Max(1, delayFromSeconds);
            return Task.Delay(milliseconds);
        }
    }
}




