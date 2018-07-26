using References;
using SavedObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit.Editors
{
    public class DeleteAllEditor : Editor
    {
        private List<DeletedWire> DeletedWires = new List<DeletedWire>();

        public override string Name => "Delete all";
        public override string Description => "delete all the wires between every group";

        public override bool IsPossible() => References.Length > 1;

        protected override void ApplyInner()
        {
            foreach (var wire in GetWires())
            {
                DeletedWires.Add(new DeletedWire(wire.Point1, wire.Point2));
                StuffDeleter.DestroyWire(wire);
            }

            SoundPlayer.PlaySoundGlobal(Sounds.DeleteSomething);
        }

        protected override IEnumerable<GameObject> CreateGhosts()
        {
            foreach (var item in GetWires())
            {
                StuffPlacer.OutlineObject(item.gameObject, OutlineColor.red);
            }

            yield break;
        }

        public override void DeletePreview()
        {
            foreach (var item in GetWires())
            {
                StuffPlacer.RemoveOutlineFromObject(item.gameObject);
            }
        }

        public override void Undo()
        {
            foreach (var item in DeletedWires)
            {
                CreateWire(item.Point1, item.Point2, false);
            }

            SoundPlayer.PlaySoundGlobal(Sounds.ConnectionFinal);
        }

        private IEnumerable<Wire> GetWires()
        {
            foreach (var groupA in References)
            {
                foreach (var groupB in References)
                {
                    if (groupA == groupB)
                        continue;

                    foreach (var wireA in groupA)
                    {
                        foreach (var wireB in groupB)
                        {
                            var wire = GetWire(wireA, wireB);

                            if (wire != null)
                                yield return wire;
                        }
                    }
                }
            }
        }

        private Wire GetWire(Transform wireA, Transform wireB)
        {
            var inputA = wireA.parent.GetComponent<CircuitInput>();
            var outputA = wireA.parent.GetComponent<CircuitOutput>();
            var inputB = wireB.parent.GetComponent<CircuitInput>();
            var outputB = wireB.parent.GetComponent<CircuitOutput>();

            Wire wire = null;

            if (inputA != null && inputB != null)
            {
                foreach (InputInputConnection ii in inputA.IIConnections)
                {
                    if ((ii.Input1 == inputA && ii.Input2 == inputB) || (ii.Input1 == inputB && ii.Input2 == inputA))
                    {
                        wire = ii;
                    }
                }
            }
            else if (inputA != null && outputB != null)
            {
                foreach (InputOutputConnection io in inputA.IOConnections)
                {
                    if (io.Input == inputA && io.Output == outputB)
                    {
                        wire = io;
                    }
                }
            }
            else if (outputA != null && inputB != null)
            {
                foreach (InputOutputConnection io in inputB.IOConnections)
                {
                    if (io.Input == inputB && io.Output == outputA)
                    {
                        wire = io;
                    }
                }
            }
            
            return wire;
        }

        private struct DeletedWire
        {
            public Transform Point1;
            public Transform Point2;

            public DeletedWire(Transform point1, Transform point2)
            {
                this.Point1 = point1 ?? throw new ArgumentNullException(nameof(point1));
                this.Point2 = point2 ?? throw new ArgumentNullException(nameof(point2));
            }
        }
    }
}
