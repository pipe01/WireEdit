using PiTung.Console;
using References;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit.Editors
{
    public abstract class Editor
    {
        public Transform[][] References { get; set; }

        private GameObject[] Created;
        private bool InPreview = false;

        public abstract string Name { get; }
        public virtual string Description { get; } = null;

        public void Apply()
        {
            InPreview = false;

            ApplyInner();
        }

        protected virtual void ApplyInner()
        {
            foreach (var wire in Created)
            {
                StuffPlacer.RemoveOutlineFromObject(wire, false);
                wire.GetComponent<Wire>().SetPegsBasedOnPoints();
                StuffConnector.LinkConnection(wire);
                StuffConnector.SetAppropriateConnectionParent(wire);
                wire.AddComponent<ObjectInfo>().ComponentType = ComponentType.Wire;
                wire.GetComponent<BoxCollider>().enabled = true;
            }

            SoundPlayer.PlaySoundGlobal(Sounds.ConnectionFinal);
        }

        public void Preview()
        {
            InPreview = true;
            Created = CreateGhosts().ToArray();
        }

        public virtual void DeletePreview()
        {
            if (Created != null && InPreview)
            {
                foreach (var item in Created)
                {
                    GameObject.Destroy(item);
                }
            }
        }

        public virtual void Undo()
        {
            if (Created != null)
            {
                foreach (var item in Created)
                {
                    StuffDeleter.DestroyWire(item);
                }

                SoundPlayer.PlaySoundGlobal(Sounds.DeleteSomething);
            }
        }

        public abstract bool IsPossible();
        protected abstract IEnumerable<GameObject> CreateGhosts();

        protected GameObject CreateWire(Transform a, Transform b, bool ghost = true)
        {
            if (WirePlacer.ConnectionExists(a.parent.gameObject, b.parent.gameObject))
                return null;

            GameObject obj = GameObject.Instantiate(Prefabs.Wire);
            Wire w;

            if (a.parent.tag == "Input" && b.parent.tag == "Input")
            {
                w = obj.AddComponent<InputInputConnection>();
                w.Point1 = a;
                w.Point2 = b;
            }
            else
            {
                w = obj.AddComponent<InputOutputConnection>();

                if (a.parent.tag == "Input")
                {
                    w.Point1 = a;
                    w.Point2 = b;
                }
                else
                {
                    w.Point1 = b;
                    w.Point2 = a;
                }
            }

            if (!WirePlacer.CanConnect(w))
            {
                GameObject.Destroy(obj);
                return null;
            }

            w.DrawWire();

            if (ghost)
            {
                obj.GetComponent<BoxCollider>().enabled = false;
                StuffPlacer.OutlineObject(obj, OutlineColor.blue);
            }
            else
            {
                w.SetPegsBasedOnPoints();
                StuffConnector.LinkConnection(w);
                StuffConnector.SetAppropriateConnectionParent(w);
                obj.AddComponent<ObjectInfo>().ComponentType = ComponentType.Wire;
                obj.GetComponent<BoxCollider>().enabled = true;
            }

            return obj;
        }
    }
}