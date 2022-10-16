using Il2CppSystem.Collections.Generic;
using System;


namespace RGActionPatches
{
    internal static class Util
    {
        internal static List<T> readOnlyToList<T>(IReadOnlyList<T> irol)
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

        internal static void addReadOnlyToList<T>(IReadOnlyList<T> irol, List<T> dest)
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

        internal static int readOnlyIndexOf<T>(IReadOnlyList<T> irol, Func<T, bool> predicate)
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

        internal static int indexOf<T>(List<T> list, Func<T, bool> predicate)
        {
            int index = 0;
            while (index < list.Count)
            {
                if (predicate.Invoke(list[index]))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
