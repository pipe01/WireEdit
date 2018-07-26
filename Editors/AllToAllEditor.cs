using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit.Editors
{
    public class AllToAllEditor : Editor
    {
        public override string Name => "All to all";
        public override string Description => "join every peg with every peg in every other group";

        protected override IEnumerable<GameObject> CreateGhosts()
        {
            for (int i = 0; i < References.Length; i++)
            {
                for (int j = 0; j < References.Length; j++)
                {
                    if (i != j)
                    {
                        foreach (var a in References[i])
                        {
                            foreach (var b in References[j])
                            {
                                yield return CreateWire(a, b);
                            }
                        }
                    }
                }
            }
        }

        public override bool IsPossible() => References.Length > 1;
    }
}
