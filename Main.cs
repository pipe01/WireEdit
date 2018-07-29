using PiTung;
using PiTung.Console;
using PiTung.Mod_utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GUILayout;

namespace WireEdit
{
    public enum States
    {
        None,
        Selecting,
        Editing
    }

    public enum Triggers
    {
        Cancel,
        Confirm,
        BeginSelect
    }

    public class WireEdit : Mod
    {
        public override string Name => "WireEdit";
        public override string PackageName => "me.pipe01.WireEdit";
        public override string Author => "pipe01";
        public override Version ModVersion => new Version("1.0.1");
        public override string UpdateUrl => "http://pipe0481.heliohost.org/pitung/mods/manifest.ptm";

        private const string SelectGuideText = "Quick guide: join two pegs to select all of the pegs in between (inclusive) and advance to the next group. Press [Z] to modify the next group (there are 4 selection groups). Left click on a peg will either add it to the current group or remove it from any group.";
        private const string EditGuideText = "Quick guide: press numbers to switch between jobs, press [ENTER] or the left mouse button to confirm. You can press [DELETE] after confirming to delete.";

        public static StateMachine<States, Triggers> State = new StateMachine<States, Triggers>(States.None);

        private readonly KeyCode[] NumericKeys = {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
        };
        private GUIStyle LabelStyle, EditorEntryStyle;
        private bool ShowGuide;
        private List<GameObject> Highlighted = new List<GameObject>();

        public override void BeforePatch()
        {
            ModInput.RegisterBinding("Select", KeyCode.L).ListenKeyDown(() => State.Fire(Triggers.BeginSelect));
            ModInput.RegisterBinding("Confirm", KeyCode.Return).ListenKeyDown(() => State.Fire(Triggers.Confirm));
            ModInput.RegisterBinding("Cancel", KeyCode.Delete).ListenKeyDown(() => State.Fire(Triggers.Cancel));
            ModInput.RegisterBinding("CycleSelection", KeyCode.Z).ListenKeyDown(() => Selection.Instance?.NextSelection());

            ShowGuide = Configuration.Get("ShowGuide", true);

            State.Configure(States.None)
                .OnEnter(ExitEditing)
                .Permit(Triggers.BeginSelect, States.Selecting)
                .Permit(Triggers.Cancel, States.None, Editing.UndoLast);

            State.Configure(States.Selecting)
                .OnEnter(() => FirstPersonInteraction.FirstPersonCamera.gameObject.AddComponent<Selection>())
                .Permit(Triggers.Cancel, States.None, ExitEditing)
                .Permit(Triggers.Confirm, States.Editing)
                .Permit(Triggers.BeginSelect, States.Selecting, LoadLastSelection);

            State.Configure(States.Editing)
                .OnEnter(EnterEditing)
                .OnExit(ExitEditing)
                .Permit(Triggers.Cancel, States.None)
                .Permit(Triggers.Confirm, States.None, () => Editing.Instance.Apply());
        }

        private bool LoadLastSelection()
        {
            Selection.Instance.LoadLastSelection();
            return false;
        }

        private void EnterEditing()
        {
            Editing.BeginEdit(Selection.Instance.GetWireReferences());
            GameObject.Destroy(Selection.Instance);
        }

        private void ExitEditing()
        {
            Editing.Instance?.Cancel();
            Highlighter.StopAll();
            GameObject.Destroy(Selection.Instance);
        }

        public override void Update()
        {
            RunWireHighlight();
            RunToggleSelection();
            RunFuckOffSelection();
            RunPollNumberKeys();
        }

        private void RunPollNumberKeys()
        {
            for (int i = 0; i < NumericKeys.Length; i++)
            {
                if (Input.GetKeyDown(NumericKeys[i]))
                {
                    if (State.CurrentState == States.Editing)
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            Editing.Instance.ReverseSelection(i);
                        }
                        else
                        {
                            Editing.Instance?.SelectEditor(i);
                        }
                    }

                    break;
                }
            }
        }

        private void RunFuckOffSelection()
        {
            if (State.CurrentState != States.None)
            {
                SelectionMenu.Instance.FuckOff();
                SelectionMenu.Instance.SelectedThing = 0;

                if (State.CurrentState == States.Editing && Input.GetMouseButtonDown(0))
                    State.Fire(Triggers.Confirm);
            }
        }

        private void RunToggleSelection()
        {
            if (Input.GetMouseButtonDown(0)
                && State.CurrentState == States.Selecting
                && Physics.Raycast(FirstPersonInteraction.Ray(), out var hit, Settings.ReachDistance, Wire.IgnoreWiresLayermask)
                && (hit.collider.tag == "Input" || hit.collider.tag == "Output"))
            {
                Selection.Instance.Toggle(hit.collider.gameObject);
            }
        }

        private void RunWireHighlight()
        {
            if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl) && State.CurrentState == States.None)
            {
                foreach (var item in Highlighted)
                {
                    Highlighter.StopHighlight(item);
                }
                Highlighted.Clear();

                if (Physics.Raycast(FirstPersonInteraction.Ray(), out var hit, Settings.ReachDistance)
                    && hit.collider.tag != "CircuitBoard")
                {
                    var wire = hit.collider.GetComponent<Wire>();

                    if (wire != null) //Clicked on wire, highlight it and both ends
                    {
                        HighlightWire(wire);
                    }
                    else
                    {
                        var input = hit.collider.GetComponent<CircuitInput>();
                        var output = hit.collider.GetComponent<CircuitOutput>();

                        if (input != null) //Clicked on input peg, highlight it and all wires connected to it
                        {
                            foreach (var item in input.IIConnections.Cast<Wire>().Concat(input.IOConnections.Cast<Wire>()))
                            {
                                HighlightWire(item);
                            }
                        }
                        else if (output != null) //Clicked on output peg, highlight it and all wires connected to it
                        {
                            foreach (var item in output.GetIOConnections())
                            {
                                HighlightWire(item);
                            }
                        }
                        else //Clicked on component, highlight inputs and outputs and all wires connected to it
                        {
                            var component = ComponentPlacer.FullComponent(hit.collider);

                            //Just get all the wires
                            foreach (var item in
                                component.GetComponentsInChildren<CircuitInput>().SelectMany(o =>
                                    o.IIConnections.Cast<Wire>().Concat(
                                    o.IOConnections.Cast<Wire>()))
                                .Concat(component.GetComponentsInChildren<CircuitOutput>().SelectMany(o =>
                                    o.GetIOConnections().Cast<Wire>())))
                            {
                                HighlightWire(item);
                            }
                        }
                    }
                }
            }

            void HighlightWire(Wire wire)
            {
                Highlight(wire.gameObject, 0);

                HighlightComponent(wire.Point1);
                HighlightComponent(wire.Point2);
            }

            void HighlightComponent(Transform wireReference)
            {
                Highlight(wireReference.parent.gameObject, 3);

                if (wireReference.parent.parent?.tag != "CircuitBoard" && wireReference.parent.parent != null)
                {
                    Highlight(wireReference.parent.parent.gameObject, 2);
                }
            }

            void Highlight(GameObject obj, int clr)
            {
                Highlighted.Add(obj);
                Highlighter.Highlight(obj, clr);
            }
        }

        public override void OnGUI()
        {
            if (LabelStyle == null)
            {
                EditorEntryStyle = new GUIStyle(GUI.skin.label)
                {
                    margin = new RectOffset(0, 0, 0, 0)
                };

                LabelStyle = new GUIStyle(EditorEntryStyle)
                {
                    fontSize = 15
                };
            }

            BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            {
                BeginVertical();
                {
                    if (State.CurrentState == States.Selecting)
                    {
                        if (ShowGuide)
                            Label(SelectGuideText, LabelStyle);

                        Label($"<b>Current group: {Selection.Instance.CurrentSelectionIndex}</b>", EditorEntryStyle);
                    }
                    else if (State.CurrentState == States.Editing)
                    {
                        if (ShowGuide)
                        {
                            Label(EditGuideText, LabelStyle);

                            Space(5);
                        }

                        for (int i = 0; i < Editing.EditorNames.Length; i++)
                        {
                            bool sel = Editing.Instance.EditorIndex == i;

                            Label($"{(sel ? "<b>" : "")} {i + 1}. {Editing.EditorNames[i]}: {Editing.EditorDescriptions[i]} {(sel ? "</b>" : "")}", EditorEntryStyle);
                        }
                    }
                }
                EndVertical();
            }
            EndArea();
        }
    }
}
