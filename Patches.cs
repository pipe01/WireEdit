using PiTung;
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
}
