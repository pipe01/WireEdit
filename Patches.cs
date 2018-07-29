using PiTung;
using PiTung.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit
{
    [Target(typeof(WirePlacer))]
    internal static class WirePlacerPatch
    {
        [PatchMethod]
        public static bool ConnectionFinal()
        {
            if (WireEdit.State.CurrentState == States.Selecting)
            {
                Selection.Instance.SaveAnotherSelection(!Input.GetKey(KeyCode.LeftControl));
                WirePlacer.DoneConnecting();
                return false;
            }

            return true;
        }
    }

    [Target(typeof(HorizontalScrollMenuWithSelection))]
    internal static class HorizontalScrollMenuWithSelectionPatch
    {
        [PatchMethod]
        public static bool ScrollThroughMenu() => WireEdit.State.CurrentState == States.None;
    }

    [Target(typeof(StuffPlacer))]
    internal static class StuffPlacerPatch
    {
        [PatchMethod]
        public static void PlaceThingBeingPlaced(ref GameObject __state)
        {
            __state = StuffPlacer.GetThingBeingPlaced;
        }

        [PatchMethod("PlaceThingBeingPlaced", PatchType.Postfix)]
        public static void PlaceThingBeingPlacedPostfix(GameObject __state)
        {
            if (Mover.IsMoving && __state != null && !Input.GetKey(KeyCode.LeftControl))
            {
                Mover.EndMove(__state);
            }
        }
    }
    
    [Target(typeof(BoardPlacer))]
    internal static class BoardPlacerPatch
    {
        [PatchMethod]
        public static void NewBoardBeingPlaced(GameObject NewBoard)
        {
            if (NewBoard != null)
                Mover.BeginMove(NewBoard);
        }
        
        [PatchMethod]
        public static void CancelPlacement() => Mover.CancelMove();
    }
}
