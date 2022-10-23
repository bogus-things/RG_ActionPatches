using RG.Scene.Action.Core;
using RG.Scene.Action.UI;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace RGActionPatches
{
    internal class StateManager: MonoBehaviour
    {
        public StateManager(System.IntPtr handle) : base(handle)
        {
            originalJobIDs = new List<int>(3);
            spoofedActors = new List<Actor>(3);
        }

        internal static StateManager Instance;

        internal CommandList currentCommandList { get; set; } = null;

        // need to track these in separate lists because Il2Cpp keeps
        // owning all my created tuples and objects
        internal List<int> originalJobIDs { get; set; }
        internal List<Actor> spoofedActors { get; set; }

        internal Actor userControlledActor { get; set; } = null;

        internal void addSpoofedActor(Actor actor, int jobID)
        {
            originalJobIDs.Add(jobID);
            spoofedActors.Add(actor);
        }

        internal void restoreSpoofedActors()
        {
            for(int i = 0; i < spoofedActors.Count; i++)
            {
                Actor actor = spoofedActors[i];
                int jobID = originalJobIDs[i];

                actor._status.JobID = jobID;
            }

            spoofedActors.Clear();
            originalJobIDs.Clear();
        }
    }
}