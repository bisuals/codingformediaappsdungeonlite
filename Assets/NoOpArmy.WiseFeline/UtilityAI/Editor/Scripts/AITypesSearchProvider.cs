using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// This search provider is used for creating the list of action/consideration types to show
    /// </summary>
    public class AITypesSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private const string GlobalNamespaceGroup = "Global namespace";

        public System.Type baseType;

        public Action<System.Type> OnTypeSelected;
        private Texture2D icon = null;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (icon == null)
                icon = Resources.Load<Texture2D>("wficon");

            List<SearchTreeEntry> list = new List<SearchTreeEntry>();

            var title = new SearchTreeGroupEntry(new GUIContent("Types"));
            title.level = 0;
            title.userData = null;
            list.Add(title);

            // Abstract and generic types cannot be created, so they are filtered out here. The runtime
            // helper stays as it is because user code may rely on getting every derived type.
            IEnumerable<Type> types = ReflectionUtilities.GetAllDerivedTypes(baseType)
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition);

            // One real group per namespace, otherwise a list sorted by namespace but drawn flat just
            // looks unsorted.
            var groups = types
                .GroupBy(t => string.IsNullOrEmpty(t.Namespace) ? GlobalNamespaceGroup : t.Namespace)
                .OrderBy(g => g.Key == GlobalNamespaceGroup ? 1 : 0)
                .ThenBy(g => g.Key, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                list.Add(new SearchTreeGroupEntry(new GUIContent(group.Key)) { level = 1 });

                foreach (var type in group.OrderBy(t => t.Name, StringComparer.Ordinal))
                {
                    var entry = new SearchTreeEntry(new GUIContent(type.Name, icon, type.FullName));
                    entry.level = 2;
                    entry.userData = type;
                    list.Add(entry);
                }
            }

            return list;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            OnTypeSelected?.Invoke((System.Type)SearchTreeEntry.userData);
            return true;
        }
    }
}
