using System;
using UnityEngine;

namespace RGActionPatches
{
    internal class StateManager : MonoBehaviour
    {
        public StateManager(IntPtr handle) : base(handle) 
        {
            Instance = this;
        }

        internal static StateManager Instance;

        internal int CurrentMapID { get; set; } = -1;
        internal int JobIDForCurrentMap { get; set; } = -2;
        internal bool CurrentMapIsPrivate { get; set; } = false;
    }
}
