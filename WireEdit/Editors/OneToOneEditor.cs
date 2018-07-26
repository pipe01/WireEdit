using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit.Editors
{
    public class OneToOneEditor : Editor
    {
        public override string Name => "One to one";
        public override string Description => "join each peg with the corresponding peg from the other group (both groups must be of the same size)";

        public override bool IsPossible() => References.Length == 2 && References.All(o => o.Length == References[0].Length);
        
        protected override IEnumerable<GameObject> CreateGhosts()
        {
            for (int i = 0; i < References[0].Length; i++)
            {
                yield return CreateWire(References[0][i], References[1][i]);
            }
        }
    }
}
