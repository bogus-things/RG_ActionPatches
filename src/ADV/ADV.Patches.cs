using BepInEx.Logging;
using RG.Scene;
using RG.Scene.Action.Core;

namespace RGActionPatches.ADV
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        private const int MInitMMFEventID = 8;
        private const int FInitMMFEventID = 9;

        internal static int SpoofForJobH(ActionScene scene, int eventID, Actor main, Actor subA = null, Actor subB = null)
        {
            int eID = eventID;
            if (scene._actionSettings.IsJobMap(scene.MapID))
            {
                int sexCount = 0;
                sexCount += main.Sex;
                sexCount += subA != null ? subA.Sex : 1;
                sexCount += subB != null ? subB.Sex : 1;

                if (sexCount == 1) // if MMF
                {
                    Actor f = main.Sex == 1 ? main : (subA.Sex == 1 ? subA : subB);
                    Actor m1 = main.Sex == 0 ? main : subA;
                    Actor m2 = main == m1 ? (subA.Sex == 0 ? subA : subB) : subB;

                    StateManager.Instance.addSpoofedActor(m1, m1.JobID, m1._status.KeyID, m1._status.IndexAsMob);
                    m1._status.JobID = f.MyBadfriendA.JobID;
                    m1._status.KeyID = f.MyBadfriendA.KeyID;
                    m1._status.IndexAsMob = f.MyBadfriendA.IndexAsMob;

                    StateManager.Instance.addSpoofedActor(m2, m2.JobID, m2._status.KeyID, m2._status.IndexAsMob);
                    m2._status.JobID = f.MyBadfriendB.JobID;
                    m2._status.KeyID = f.MyBadfriendB.KeyID;
                    m2._status.IndexAsMob = f.MyBadfriendB.IndexAsMob;

                    eID = f == main ? FInitMMFEventID : MInitMMFEventID;
                }
                else
                {
                    StateManager.Instance.addSpoofedActor(main, main.JobID, main._status.KeyID, main._status.IndexAsMob);
                    Util.SpoofJobID(scene, main);

                    if (subA != null)
                    {
                        StateManager.Instance.addSpoofedActor(subA, subA.JobID, subA._status.KeyID, subA._status.IndexAsMob);
                        Util.SpoofJobID(scene, subA);
                    }

                    if (subB != null)
                    {
                        StateManager.Instance.addSpoofedActor(subB, subB.JobID, subB._status.KeyID, subB._status.IndexAsMob);
                        Util.SpoofJobID(scene, subB);
                    }
                }
               
            }
            return eID;
        }

        internal static Actor PatchActorsForPrivateMMF(ActionScene scene, int eventID, Actor main, Actor subA, Actor subB)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                if ((eventID == MInitMMFEventID || eventID == FInitMMFEventID) && subB == null)
                {
                    foreach(Actor m in scene._maleActors)
                    {
                        if (m.name != main.name && m.name != subA.name)
                        {
                            return m;
                        }
                    }
                }
            }

            return subB;
        }

        internal static int SpoofForPrivateH(ActionScene scene, int eventID, Actor main, Actor subA = null, Actor subB = null)
        {
            int eID = eventID;
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                int sexCount = 0;
                sexCount += main.Sex;
                sexCount += subA != null ? subA.Sex : 1;
                sexCount += subB != null ? subB.Sex : 1;

                if (sexCount == 1) // if MMF
                {
                    Actor f = main.Sex == 1 ? main : (subA.Sex == 1 ? subA : subB);
                    Actor m1 = main.Sex == 0 ? main : subA;
                    Actor m2 = main == m1 ? (subA.Sex == 0 ? subA : subB) : subB;

                    StateManager.Instance.addSpoofedActor(m1, m1.JobID, m1._status.KeyID, m1._status.IndexAsMob);
                    m1._status.JobID = f.MyBadfriendA.JobID;
                    m1._status.KeyID = f.MyBadfriendA.KeyID;
                    m1._status.IndexAsMob = f.MyBadfriendA.IndexAsMob;

                    StateManager.Instance.addSpoofedActor(m2, m2.JobID, m2._status.KeyID, m2._status.IndexAsMob);
                    m2._status.JobID = f.MyBadfriendB.JobID;
                    m2._status.KeyID = f.MyBadfriendB.KeyID;
                    m2._status.IndexAsMob = f.MyBadfriendB.IndexAsMob;

                    eID = f == main ? FInitMMFEventID : MInitMMFEventID;
                }
            }
            return eID;
        }

        internal static void RestoreSpoofed()
        {
            StateManager.Instance.restoreSpoofedActors();
        }

        internal static void RedirectMissingAssets(ref string bundle, ref string asset)
        {
            string[] parts = bundle.Split('/');

            if (parts.Length >= 2 && parts[parts.Length - 1] == "h_special.unity3d")
            {
                string cha = parts[parts.Length - 2];

                // case: office desk special H intro
                if (asset == "102" && !(cha == "c00" || cha == "c01"))
                {
                    bundle = $"adv/scenario/00_01/{cha}/h_normal.unity3d";
                    asset = "0";
                }
                // case: office desk special H outro
                else if (asset == "103" && !(cha == "c00" || cha == "c01"))
                {
                    bundle = $"adv/scenario/00_01/{cha}/h_normal.unity3d";
                    asset = "16";
                }
                // case: class desk special H intro
                else if (asset == "118" && !(cha == "c04" || cha == "c05"))
                {
                    bundle = $"adv/scenario/00_01/{cha}/h_normal.unity3d";
                    asset = "0";
                }
                // case: class desk special H outro
                else if (asset == "119" && !(cha == "c04" || cha == "c05"))
                {
                    bundle = $"adv/scenario/00_01/{cha}/h_normal.unity3d";
                    asset = "16";
                }
            }
        }
    }
}
