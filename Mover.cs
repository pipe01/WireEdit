using PiTung.Console;
using References;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit
{
    public static class Mover
    {
        private static readonly List<Connection> Connections = new List<Connection>();

        public static bool IsMoving { get; private set; }

        public static IEnumerable<Wire> GetWires(GameObject obj) =>
            obj.GetComponentsInChildren<CircuitInput>().SelectMany(o =>
                o.IIConnections.Cast<Wire>().Concat(
                o.IOConnections.Cast<Wire>()))
            .Concat(obj.GetComponentsInChildren<CircuitOutput>().SelectMany(o =>
                o.GetIOConnections().Cast<Wire>()));

        public static void BeginMove(GameObject obj)
        {
            IsMoving = true;

            var inputs = obj.GetComponentsInChildren<CircuitInput>();
            var outputs = obj.GetComponentsInChildren<CircuitOutput>();

            Connections.Clear();

            foreach (var input in inputs)
            {
                foreach (var wire in input.IIConnections.Cast<Wire>().Concat(input.IOConnections.Cast<Wire>()))
                {
                    var point = ComponentPlacer.FullComponent(wire.Point1) == obj ? wire.Point2 : wire.Point1;
                    var theOtherPoint = point == wire.Point1 ? wire.Point2 : wire.Point1;

                    if (CheckPointParent(wire.Point1) != CheckPointParent(wire.Point2))
                        Connections.Add(new Connection(Array.IndexOf(inputs, input), theOtherPoint, wire is InputInputConnection, false));
                }
            }

            foreach (var output in outputs)
            {
                foreach (var wire in output.GetIOConnections())
                {
                    var point = ComponentPlacer.FullComponent(wire.Point1) == obj ? wire.Point2 : wire.Point1;

                    if (CheckPointParent(wire.Point1) != CheckPointParent(wire.Point2))
                        Connections.Add(new Connection(Array.IndexOf(outputs, output), point, false, true));
                }
            }
            
            bool CheckPointParent(Transform point)
            {
                Transform p = point;
                while (p != null)
                {
                    if (p == obj.transform)
                        return false;

                    p = p.parent;
                }

                return true;
            }
        }

        public static void EndMove(GameObject newObj)
        {
            var inputs = newObj.GetComponentsInChildren<CircuitInput>();
            var outputs = newObj.GetComponentsInChildren<CircuitOutput>();

            foreach (var item in Connections)
            {
                GameObject obj = GameObject.Instantiate(Prefabs.Wire);

                var wire = item.InputInput ? (Wire)obj.AddComponent<InputInputConnection>() : obj.AddComponent<InputOutputConnection>();

                var a = (item.OutputIsMoved ? outputs[item.Index].transform : inputs[item.Index].transform).Find("WireReference");
                var b = item.Point;

                if (a.parent.tag == "Input")
                {
                    wire.Point1 = a;
                    wire.Point2 = b;
                }
                else
                {
                    wire.Point1 = b;
                    wire.Point2 = a;
                }
                
                if (!WirePlacer.CanConnect(wire))
                {
                    GameObject.Destroy(obj);
                    continue;
                }

                wire.DrawWire();
                wire.SetPegsBasedOnPoints();
                StuffConnector.LinkConnection(wire);
                StuffConnector.SetAppropriateConnectionParent(wire);
                obj.AddComponent<ObjectInfo>().ComponentType = ComponentType.Wire;
                obj.GetComponent<BoxCollider>().enabled = true;
            }

            IsMoving = false;
        }

        public static void CancelMove() => IsMoving = false;

        private struct Connection
        {
            public int Index;
            public Transform Point;
            public bool OutputIsMoved;
            public bool InputInput;

            public Connection(int index, Transform point, bool ii, bool outputIsMoved)
            {
                this.Index = index;
                this.Point = point ?? throw new ArgumentNullException(nameof(point));
                this.InputInput = ii;
                this.OutputIsMoved = outputIsMoved;
            }
        }
    }
}
