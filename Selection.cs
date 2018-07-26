using PiTung;
using PiTung.Console;
using PiTung.Mod_utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit
{
    public class Selection : MonoBehaviour
    {
        public static Selection Instance { get; private set; }

        private static Transform[][] LastSelection;

        public const int MaxSelections = 4;

        public int CurrentSelectionIndex { get; private set; } = 0;
        
        private List<List<GameObject>> Selections = new List<List<GameObject>>();

        void Start()
        {
            Instance = this;

            //Add 4 empty lists
            Selections.AddRange(Enumerable.Range(0, MaxSelections).Select(_ => new List<GameObject>()));
        }
        
        void OnDestroy()
        {
            LastSelection = GetWireReferences();
            Instance = null;
        }

        public void LoadLastSelection()
        {
            if (LastSelection != null)
            {
                Selections = LastSelection.Select(o => o.Select(i => i.parent?.gameObject).ToList()).ToList();

                for (int i = 0; i < Selections.Count; i++)
                {
                    foreach (var item in Selections[i])
                    {
                        Highlighter.Highlight(item, i);
                    }
                }

                LastSelection = null;
            }
        }

        public void SaveAnotherSelection(bool advance = true)
        {
            GameObject a = ModUtilities.GetStaticFieldValue<GameObject>(typeof(WirePlacer), "SelectedPeg");
            GameObject b = ModUtilities.GetStaticFieldValue<GameObject>(typeof(WirePlacer), "PegBeingLookedAt");

            Vector3 point1, point2;
            
            point1 = a.transform.GetChild(0).position;
            point2 = b.transform.GetChild(0).position;

            var l = Selections[CurrentSelectionIndex];
            
            if (!IsSelected(a))
            {
                l.Add(a);
                Highlighter.Highlight(a, CurrentSelectionIndex);
            }

            foreach (var item in Physics.RaycastAll(point1, point2 - point1, Vector3.Distance(point1, point2)))
            {
                if (item.collider.tag == "Input" || item.collider.tag == "Output")
                {
                    if (IsSelected(item.collider.gameObject))
                        continue;

                    l.Add(item.collider.gameObject);
                    Highlighter.Highlight(item.collider.gameObject, CurrentSelectionIndex);
                }
            }
            
            if (advance)
                NextSelection();
        }

        private bool IsSelected(GameObject obj) => Selections.Any(o => o.Any(i => i == obj));

        public void Toggle(GameObject obj)
        {
            if (IsSelected(obj))
            {
                Selections.Single(o => o.Contains(obj)).Remove(obj);
                Highlighter.StopHighlight(obj);
            }
            else
            {
                Selections[CurrentSelectionIndex].Add(obj);
                Highlighter.Highlight(obj, CurrentSelectionIndex);
            }
        }

        public void NextSelection()
        {
            if (CurrentSelectionIndex < MaxSelections - 1)
                CurrentSelectionIndex++;
            else
                CurrentSelectionIndex = 0;
        }
        
        public Transform[][] GetWireReferences()
        {
            return Selections
                .Select(o =>
                {
                    if (o.Count == 0)
                        return new Transform[0];

                    var l = o.Select(Wire.GetWireReference).ToList();
                    var first = l[0];
                    l.Sort((c1, c2) => Vector3.Distance(first.position, c1.position).CompareTo(Vector3.Distance(first.position, c2.position)));
                    
                    return l.ToArray();
                })
                .Where(o => o.Length > 0)
                .ToArray();
        }

        public void Cancel()
        {
            Selections.Clear();
        }
    }
}
