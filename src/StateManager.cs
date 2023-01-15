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
            originalKeyIDs = new List<int>(3);
            originalMobIndexes = new List<byte>(3);
        }

        internal static StateManager Instance;

        internal CommandList currentCommandList { get; set; } = null;

        // need to track these in separate lists because Il2Cpp keeps
        // yeeting all my created tuples and objects
        internal List<int> originalJobIDs { get; set; }
        internal List<Actor> spoofedActors { get; set; }
        internal List<int> originalKeyIDs { get; set; }
        internal List<byte> originalMobIndexes { get; set; }

        internal Actor userControlledActor { get; set; } = null;
        internal Actor guestActor { get; set; } = null;
        internal Actor redirectedGuestActor { get; set; } = null;
        internal bool livingRoomGuestSpoof { get; set; } = false;
        internal bool badfriendSpoof { get; set; } = false;

        internal void addSpoofedActor(Actor actor, int jobID, int keyID, byte indexAsMob)
        {
            if (!spoofedActors.Contains(actor))
            {
                originalJobIDs.Add(jobID);
                spoofedActors.Add(actor);
                originalKeyIDs.Add(keyID);
                originalMobIndexes.Add(indexAsMob);
            }
            
        }

        internal void restoreSpoofedActors()
        {
            for(int i = 0; i < spoofedActors.Count; i++)
            {
                Actor actor = spoofedActors[i];
                int jobID = originalJobIDs[i];
                int keyID = originalKeyIDs[i];
                byte indexAsMob = originalMobIndexes[i];

                actor._status.JobID = jobID;
                actor._status.KeyID = keyID;
                actor._status.IndexAsMob = indexAsMob;
            }

            spoofedActors.Clear();
            originalJobIDs.Clear();
        }
    }
}