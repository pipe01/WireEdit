using PiTung.Console;
using References;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WireEdit.Editors;

namespace WireEdit
{
    public class Editing
    {
        public static Editing Instance { get; private set; }

        private static Editor LastEditor;
        private static int LastEditorIndex;
        private static readonly IList<Editor> Editors = new List<Editor>();
        public static readonly string[] EditorNames;
        public static readonly string[] EditorDescriptions;
        
        private Transform[][] References;

        private Editor SelectedEditor;

        public int EditorIndex => Editors.IndexOf(SelectedEditor);

        static Editing()
        {
            Add<AllToOneEditor>();
            Add<AllToAllEditor>();
            Add<OneToOneEditor>();
            Add<DeleteAllEditor>();

            EditorNames = Editors.Select(o => o.Name).ToArray();
            EditorDescriptions = Editors.Select(o => o.Description).ToArray();

            void Add<T>() where T : Editor => Editors.Add((T)Activator.CreateInstance(typeof(T)));
        }

        public Editing(Transform[][] references)
        {
            SetReferences(references);

            SelectEditor(LastEditorIndex);
        }
        
        private void SetReferences(Transform[][] references)
        {
            References = references;

            foreach (var item in Editors)
            {
                item.References = references;
            }
        }

        public bool Apply()
        {
            if (!SelectedEditor.IsPossible())
            {
                SoundPlayer.PlaySoundGlobal(Sounds.FailDoSomething);
                return false;
            }
            
            SelectedEditor.Apply();

            LastEditor = SelectedEditor;

            Instance = null;

            return true;
        }

        public void SelectEditor(int index)
        {
            LastEditorIndex = index;
            SelectedEditor?.DeletePreview();
            SelectedEditor = Editors[Mathf.Clamp(index, 0, Editors.Count - 1)];

            if (SelectedEditor.IsPossible())
                SelectedEditor.Preview();
        }

        public void RefreshPreview()
        {
            SelectedEditor.DeletePreview();
            
            if (SelectedEditor.IsPossible())
                SelectedEditor.Preview();
        }

        public void ReverseSelection(int index)
        {
            if (index < References.Length)
            {
                References[index] = References[index].Reverse().ToArray();

                SetReferences(References);
                RefreshPreview();
            }
        }

        public void Cancel()
        {
            SelectedEditor.DeletePreview();

            Instance = null;
        }

        public static void UndoLast()
        {
            if (LastEditor != null)
            {
                LastEditor.Undo();
                LastEditor = null;
            }
        }

        public static void BeginEdit(Transform[][] references)
        {
            Instance = new Editing(references);
        }
    }
}
