using System.Collections.Generic;

namespace NoOpArmy.WiseFeline
{
    public static class EditorExtensions
    {
        /// <summary>
        /// Swaps two items of a list. Both items have to be in the list.
        /// </summary>
        public static IList<T> Swap<T>(this IList<T> list, T itemA, T itemB)
        {
            if (!list.Contains(itemA) || !list.Contains(itemB))
            {
                throw new System.Exception("one or both items are not in the list");
            }

            int indexA = list.IndexOf(itemA);
            int indexB = list.IndexOf(itemB);
            list[indexA] = itemB;
            list[indexB] = itemA;
            return list;
        }

        /// <summary>
        /// Moves an item to another index of the list, pushing the items in between one place up or
        /// down. This is what a list reorder does, unlike Swap which only exchanges two items.
        /// </summary>
        /// <param name="list">The list to change.</param>
        /// <param name="item">The item to move. Nothing happens when it is not in the list.</param>
        /// <param name="newIndex">The index the item should end up at, clamped to the list.</param>
        public static IList<T> Move<T>(this IList<T> list, T item, int newIndex)
        {
            int oldIndex = list.IndexOf(item);
            if (oldIndex < 0)
                return list;

            if (newIndex < 0)
                newIndex = 0;
            if (newIndex > list.Count - 1)
                newIndex = list.Count - 1;

            if (newIndex == oldIndex)
                return list;

            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            return list;
        }
    }
}
