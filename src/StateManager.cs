using RG.Scene.Action.UI;
using System;
using UnityEngine;

namespace RGActionPatches
{
    internal class StateManager: MonoBehaviour
    {
        public StateManager(IntPtr handle) : base(handle)
        {
            Instance = this;
        }

        internal static StateManager Instance;

        internal CommandList currentCommandList { get; set; } = null;
    }
}