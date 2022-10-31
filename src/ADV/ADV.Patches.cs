using RG.Scene;
using RG.Scene.Action.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RGActionPatches.ADV
{
    class Patches
    {
        internal static void SpoofForJobH(ActionScene scene, Actor main, Actor subA = null, Actor subB = null)
        {
            if (scene._actionSettings.IsJobMap(scene.MapID))
            {
                StateManager.Instance.addSpoofedActor(main, main.JobID);
                Util.SpoofJobID(scene, main);

                if (subA != null)
                {
                    StateManager.Instance.addSpoofedActor(subA, subA.JobID);
                    Util.SpoofJobID(scene, subA);
                }

                if (subB != null)
                {
                    StateManager.Instance.addSpoofedActor(subB, subB.JobID);
                    Util.SpoofJobID(scene, subB);
                }
            }
        }

        internal static void RestoreSpoofed()
        {
            for (int i = 0; i < StateManager.Instance.spoofedActors.Count; i++)
            {
                Actor actor = StateManager.Instance.spoofedActors[i];
                int jobID = StateManager.Instance.originalJobIDs[i];
            }
            StateManager.Instance.restoreSpoofedActors();
        }
    }
}
