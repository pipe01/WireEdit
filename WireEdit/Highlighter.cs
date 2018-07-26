using cakeslice;
using PiTung;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WireEdit
{
    public static class Highlighter
    {
        private static readonly Color[] Colors = new Color[] {
            Color.blue,
            new Color32(239, 127, 26, 255), //Orange
            Color.green,
            Color.yellow
        };

        private static readonly Material[] Materials;

        private static Dictionary<GameObject, HighlightComp> Lights = new Dictionary<GameObject, HighlightComp>();

        static Highlighter()
        {
            Materials = new Material[Colors.Length];

            Shader outlineShader = ModUtilities.GetFieldValue<Shader>(OutlineEffect.Instance, "outlineShader");

            for (int i = 0; i < Colors.Length; i++)
            {
                Material mat = new Material(outlineShader);

                Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                tex.SetPixel(0, 0, Colors[i]);
                tex.Apply();

                mat.SetTexture("_MainTex", tex);
                mat.renderQueue = 5000;

                Materials[i] = mat;
            }
        }

        public static void Highlight(GameObject target, int color)
        {
            if (Lights.TryGetValue(target, out var h))
                StopHighlight(target);

            var comp = target.AddComponent<HighlightComp>();
            comp.Material = Materials[color];

            Lights.Add(target, comp);
        }

        public static void StopHighlight(GameObject obj)
        {
            if (Lights.TryGetValue(obj, out var h))
            {
                GameObject.Destroy(h);
                Lights.Remove(obj);
            }
        }

        public static void StopAll()
        {
            foreach (var item in Lights)
            {
                GameObject.Destroy(item.Value);
            }

            Lights.Clear();
        }

        private class HighlightComp : MonoBehaviour
        {
            private GameObject LightObject;

            public Material Material;

            void Start()
            {
                GameObject obj = new GameObject();
                obj.transform.parent = transform;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                obj.AddComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().mesh;
                obj.AddComponent<MeshRenderer>().material = new Material(Material);

                LightObject = obj;
            }

            void OnDestroy()
            {
                GameObject.Destroy(LightObject);
            }
        }
    }
}
