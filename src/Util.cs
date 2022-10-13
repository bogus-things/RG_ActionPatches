using Il2CppSystem.Collections.Generic;
using System;

namespace RGActionPatches
{
    internal static class Util
    {
        internal static List<T> ReadOnlyToList<T>(IReadOnlyList<T> irol)
        {
            List<T> list = new List<T>();
            int i = 0;
            T item;
            try
            {
                item = irol[i];
            }
            catch (Exception)
            {
                return list;
            }

            while(true)
            {
                list.Add(item);
                i++;
                try
                {
                    item = irol[i];
                }
                catch (Exception)
                {
                    break;
                }
            }

            return list;
        }

        internal static void AddReadOnlyToList<T>(IReadOnlyList<T> irol, List<T> dest)
        {
            int i = 0;
            T item;
            try
            {
                item = irol[i];
            }
            catch (Exception)
            {
                return;
            }

            while (true)
            {
                dest.Add(item);
                i++;
                try
                {
                    item = irol[i];
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        internal static int getWorkerJobIDForMap(int currentJobID)
        {
            int newID = currentJobID;
            int workerJobID = StateManager.Instance.JobIDForCurrentMap;
            // For everyone but Customer, Badfriend, Common, and not in private areas
            if (currentJobID > -1 && !StateManager.Instance.CurrentMapIsPrivate && workerJobID > -1)
            {
                newID = workerJobID;
            }
            return newID;
        }

        internal static int readOnlyIndexOf<T>(IReadOnlyList<T> irol, Il2CppSystem.Predicate<T> predicate)
        {
            int i = 0;
            T item;

            while (true)
            {
                try
                {
                    item = irol[i];
                    if (predicate.Invoke(item))
                    {
                        return i;
                    }
                    i++;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }
    }
}
