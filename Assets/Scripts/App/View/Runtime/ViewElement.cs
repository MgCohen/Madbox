using System.ComponentModel;
using UnityEngine;
using Scaffold.Navigation.Contracts;
using Scaffold.MVVM.Binding;
using System.Collections.Generic;
using System;
using Scaffold.MVVM.Contracts;
namespace Scaffold.MVVM
{
    [BindSource(typeof(TreeBinding))]
    public abstract partial class ViewElement : MonoBehaviour
    {
        protected void OnViewModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e == null)
{
    return;
}
            var elementTypeName = GetType().Name;
            Debug.Log("View element update : " + elementTypeName + " - " + e.PropertyName);
            var bindSourceName = GetBindSourceName();
            var propertyFullName = string.Join('.', bindSourceName, e.PropertyName);
            UpdateBinding(propertyFullName);
        }

        protected abstract string GetBindSourceName();

        public virtual void Bind(IViewController viewModel)
        {
            if (viewModel == null)
{
    return;
}
        }

        protected virtual void OnBind()
        {

        }

        protected void Unbind()
        {
            ClearBindings();
            OnUnbind();
        }

        protected virtual void OnUnbind()
        {

        }
    }

    public abstract class ViewElement<T> : ViewElement where T : IViewModel
    {
        [SerializeField]
        protected T viewModel;

        public sealed override void Bind(IViewController viewController)
        {
            T vm = viewController switch { T typed => typed, null => default, _ => throw new Exception($"Trying to bind view {GetType()} to controller of type {viewController.GetType()}, expected: {typeof(T)}") };
            if (!EqualityComparer<T>.Default.Equals(viewModel, default)) Unbind();
            this.viewModel = vm;
            Debug.Log("Registering view model " + GetType().Name + " - " + typeof(T).Name);
            if (vm is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= OnViewModelChanged;
                npc.PropertyChanged += OnViewModelChanged;
            }
            if (vm is INestedObservableProperties nop) nop.RegisterNestedProperties();
            OnBind();
        }

        protected sealed override string GetBindSourceName()
        {
            return nameof(viewModel);
        }
    }

    public abstract class ViewElement<T, J> : ViewElement<J> where T: ViewElement where J : IViewModel
    {
        protected T parent;

        public void Bind(T parent, J viewModel)
        {
            if (parent == null)
{
    throw new System.ArgumentNullException(nameof(parent));
}
            if (viewModel == null)
{
    return;
}
            this.parent = parent;
            Bind(viewModel);
        }

    }
}







