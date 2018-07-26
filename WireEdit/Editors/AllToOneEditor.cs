using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit.Editors
{
    public class AllToOneEditor : Editor
    {
        public override string Name => "All to one";
        public override string Description => "join all the pegs in the first group to the second group's single peg";

        protected override IEnumerable<GameObject> CreateGhosts()
        {
            return References[0].Select(o => CreateWire(References[1][0], o));
        }

        public override bool IsPossible() => References.Length == 2 && References[1].Length == 1;
    }
}
