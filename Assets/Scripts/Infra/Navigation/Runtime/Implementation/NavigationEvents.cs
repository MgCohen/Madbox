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
    public record BeforeViewCloseEvent(Type ViewType) : ContextEvent;

    public record AfterViewCloseEvent(Type ViewType) : ContextEvent;

    public record BeforeViewOpenEvent(Type ViewType) : ContextEvent;

    public record AfterViewOpenEvent(Type ViewType) : ContextEvent;
}



