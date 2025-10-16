using System.Collections.Generic;
using UnityEngine;

namespace Ninjadini.Console
{
    public class ConsoleOptionsCatalogAutoRemove : MonoBehaviour
    {
        public List<(Component component, ConsoleOptions.Catalog catalog)> Catalogs;

        public void Add(Component component, ConsoleOptions.Catalog catalog)
        {
            Catalogs ??= new List<(Component component, ConsoleOptions.Catalog catalog)>();
            for (var i = Catalogs.Count - 1; i >= 0; i--)
            {
                var group = Catalogs[i];
                if (group.component == component)
                {
                    group.catalog?.RemoveAll();
                    group.catalog = catalog;
                    Catalogs[i] = group;
                    return;
                }
            }
            Catalogs.Add((component, catalog));
        }
        
        void OnDestroy()
        {
            if (Catalogs != null)
            {
                foreach (var group in Catalogs)
                {
                    group.catalog.RemoveAll();
                }
                Catalogs = null;
            }
        }
    }
}