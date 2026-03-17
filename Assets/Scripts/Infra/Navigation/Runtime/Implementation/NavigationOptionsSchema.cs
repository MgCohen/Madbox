using UnityEngine;
using Scaffold.Types;
using Scaffold.Events.Contracts;
using Scaffold.Events;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Scaffold.Navigation.Contracts;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace Scaffold.Navigation
{
    internal class NavigationOptionsSchema : ViewSchema
    {
        public NavigationOptions Options => options;
        [SerializeField] private NavigationOptions options;
    }
}


