#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fjordfall.EditorTools
{
    /// <summary>
    /// Recreates the small original OBJ pack when text model files are absent.
    /// The manual ZIP already contains the baked OBJ/MTL files. This source generator
    /// keeps Git checkouts reproducible without storing large imported-library folders.
    /// </summary>
    [InitializeOnLoad]
    public static class FjordfallGeneratedModelBuilder
    {
        private const string TargetDir = "Assets/Resources/External/Models";

        static FjordfallGeneratedModelBuilder()
        {
            EditorApplication.delayCall += EnsureModels;
        }

        [MenuItem("Fjordfall/5. 외부 모델 재생성", priority = 5)]
        public static void EnsureModels()
        {
            Directory.CreateDirectory(TargetDir);
            bool wrote = false;
            wrote |= Write("fjord_house", House(), Mtl(
                ("Plaster", "village_plaster.png"), ("Roof", "roof_shingle.png"), ("Wood", "dark_bark.png")));
            wrote |= Write("fjord_tree", Tree(), Mtl(
                ("Bark", "dark_bark.png"), ("Foliage", "dark_foliage.png")));
            wrote |= Write("fjord_longboat", Boat(), Mtl(
                ("Wood", "dark_bark.png"), ("Sail", "linen_sail.png")));
            wrote |= Write("fjord_wall", Wall(), Mtl(("Stone", "cliff_stone.png")));
            wrote |= Write("fjord_rock", Rock(), Mtl(("Stone", "cliff_stone.png")));
            wrote |= Write("fjord_soldier", Soldier(), Mtl(
                ("Cloth", "linen_sail.png"), ("Skin", "village_plaster.png"), ("Metal", "iron_worn.png")));
            wrote |= Write("fjord_shield", Shield(), Mtl(("Metal", "iron_worn.png")));
            if (wrote) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static bool Write(string name, string objBody, string mtl)
        {
            string objPath = Path.Combine(TargetDir, name + ".obj");
            string mtlPath = Path.Combine(TargetDir, name + ".mtl");
            bool wrote = false;
            if (!File.Exists(mtlPath)) { File.WriteAllText(mtlPath, mtl, Encoding.UTF8); wrote = true; }
            if (!File.Exists(objPath))
            {
                File.WriteAllText(objPath, "# Fjordfall original low-poly asset\nmtllib " + name + ".mtl\no " + name + "\n" + objBody, Encoding.UTF8);
                wrote = true;
            }
            if (wrote) Debug.Log("[Fjordfall] Generated external OBJ: " + name);
            return wrote;
        }

        private static string Mtl(params (string name, string texture)[] materials)
        {
            StringBuilder s = new StringBuilder("# Fjordfall original materials\n");
            foreach (var m in materials)
            {
                s.Append("newmtl ").Append(m.name).Append("\nKd 1 1 1\nKa 0.2 0.2 0.2\nKs 0.05 0.05 0.05\nNs 8\nmap_Kd ../Textures/")
                    .Append(m.texture).Append("\n\n");
            }
            return s.ToString();
        }

        private static string House()
        {
            Obj b = new Obj();
            b.Box("Plaster", new Vector3(0f, .55f, 0f), new Vector3(1.5f, 1.1f, 1.15f));
            b.Roof("Roof", new Vector3(0f, 1.32f, 0f), 1.78f, 1.45f, .78f);
            b.Box("Wood", new Vector3(0f, .48f, -.59f), new Vector3(.36f, .78f, .06f));
            return b.ToString();
        }

        private static string Tree()
        {
            Obj b = new Obj();
            b.Cylinder("Bark", new Vector3(0f, .55f, 0f), .17f, 1.1f, 8);
            b.Cone("Foliage", new Vector3(0f, 1.24f, 0f), .95f, 1.25f, 9);
            b.Cone("Foliage", new Vector3(0f, 1.76f, 0f), .74f, 1.05f, 9);
            b.Cone("Foliage", new Vector3(0f, 2.15f, 0f), .50f, .72f, 9);
            return b.ToString();
        }

        private static string Boat()
        {
            Obj b = new Obj();
            b.Wedge("Wood", new Vector3(0f, .25f, 0f), 1.18f, .55f, 3.4f);
            b.Cylinder("Wood", new Vector3(0f, 1.08f, 0f), .055f, 1.75f, 8);
            b.Quad("Sail", new Vector3(-.04f, 1.35f, 0f), new Vector3(1.55f, 1.05f, .03f));
            return b.ToString();
        }

        private static string Wall()
        {
            Obj b = new Obj();
            b.Box("Stone", new Vector3(0f, .48f, 0f), new Vector3(2.4f, .96f, .42f));
            for (int i = 0; i < 4; i++) b.Box("Stone", new Vector3(-.9f + i * .6f, 1.12f, 0f), new Vector3(.38f, .34f, .5f));
            return b.ToString();
        }

        private static string Rock()
        {
            Obj b = new Obj();
            b.IrregularRock("Stone", new Vector3(0f, .35f, 0f), new Vector3(1.25f, .72f, 1.0f));
            return b.ToString();
        }

        private static string Soldier()
        {
            Obj b = new Obj();
            b.Box("Cloth", new Vector3(0f, .48f, 0f), new Vector3(.48f, .76f, .34f));
            b.Cylinder("Skin", new Vector3(0f, .98f, 0f), .24f, .36f, 10);
            b.Cone("Metal", new Vector3(0f, 1.23f, 0f), .29f, .38f, 10);
            b.Box("Cloth", new Vector3(0f, .18f, 0f), new Vector3(.54f, .25f, .40f));
            return b.ToString();
        }

        private static string Shield()
        {
            Obj b = new Obj();
            b.Cylinder("Metal", Vector3.zero, .50f, .12f, 12, Quaternion.Euler(90f, 0f, 0f));
            return b.ToString();
        }

        private sealed class Obj
        {
            private readonly StringBuilder s = new StringBuilder();
            private int vertex;
            private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

            private int V(Vector3 p)
            {
                s.Append("v ").Append(F(p.x)).Append(' ').Append(F(p.y)).Append(' ').Append(F(p.z)).Append('\n');
                return ++vertex;
            }

            private void Face(string material, params int[] indices)
            {
                s.Append("usemtl ").Append(material).Append('\n').Append("f");
                foreach (int i in indices) s.Append(' ').Append(i);
                s.Append('\n');
            }

            public void Box(string material, Vector3 center, Vector3 size)
            {
                Vector3 h = size * .5f;
                int[] v =
                {
                    V(center + new Vector3(-h.x,-h.y,-h.z)), V(center + new Vector3(h.x,-h.y,-h.z)),
                    V(center + new Vector3(h.x,h.y,-h.z)), V(center + new Vector3(-h.x,h.y,-h.z)),
                    V(center + new Vector3(-h.x,-h.y,h.z)), V(center + new Vector3(h.x,-h.y,h.z)),
                    V(center + new Vector3(h.x,h.y,h.z)), V(center + new Vector3(-h.x,h.y,h.z))
                };
                Face(material, v[0],v[1],v[2],v[3]); Face(material, v[5],v[4],v[7],v[6]);
                Face(material, v[4],v[0],v[3],v[7]); Face(material, v[1],v[5],v[6],v[2]);
                Face(material, v[3],v[2],v[6],v[7]); Face(material, v[4],v[5],v[1],v[0]);
            }

            public void Roof(string material, Vector3 center, float width, float depth, float height)
            {
                float x = width * .5f, z = depth * .5f, y = height * .5f;
                int a=V(center+new Vector3(-x,-y,-z)), b=V(center+new Vector3(x,-y,-z));
                int c=V(center+new Vector3(x,-y,z)), d=V(center+new Vector3(-x,-y,z));
                int e=V(center+new Vector3(0,y,-z)), f=V(center+new Vector3(0,y,z));
                Face(material,a,b,e); Face(material,d,f,c); Face(material,a,e,f,d); Face(material,b,c,f,e); Face(material,a,d,c,b);
            }

            public void Wedge(string material, Vector3 center, float width, float height, float length)
            {
                float x=width*.5f, y=height*.5f, z=length*.5f;
                int a=V(center+new Vector3(0,-y,-z)), b=V(center+new Vector3(-x,-y,z*.55f)), c=V(center+new Vector3(x,-y,z*.55f));
                int d=V(center+new Vector3(0,y,-z*.88f)), e=V(center+new Vector3(-x*.78f,y,z*.44f)), f=V(center+new Vector3(x*.78f,y,z*.44f));
                Face(material,a,c,b); Face(material,d,e,f); Face(material,a,b,e,d); Face(material,a,d,f,c); Face(material,b,c,f,e);
            }

            public void Quad(string material, Vector3 center, Vector3 size)
            {
                Vector3 h=size*.5f;
                int a=V(center+new Vector3(-h.x,-h.y,0)), b=V(center+new Vector3(h.x,-h.y,0));
                int c=V(center+new Vector3(h.x,h.y,0)), d=V(center+new Vector3(-h.x,h.y,0));
                Face(material,a,b,c,d); Face(material,d,c,b,a);
            }

            public void Cylinder(string material, Vector3 center, float radius, float height, int sides, Quaternion rotation = default)
            {
                if (rotation == default) rotation = Quaternion.identity;
                int[] bottom=new int[sides], top=new int[sides];
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2f/sides;
                    bottom[i]=V(center+rotation*new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius));
                    top[i]=V(center+rotation*new Vector3(Mathf.Cos(a)*radius,height*.5f,Mathf.Sin(a)*radius));
                }
                for(int i=0;i<sides;i++) Face(material,bottom[i],bottom[(i+1)%sides],top[(i+1)%sides],top[i]);
                Face(material,top); Array.Reverse(bottom); Face(material,bottom);
            }

            public void Cone(string material, Vector3 center, float radius, float height, int sides)
            {
                int[] ring=new int[sides];
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2f/sides;
                    ring[i]=V(center+new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius));
                }
                int tip=V(center+Vector3.up*height*.5f);
                for(int i=0;i<sides;i++) Face(material,ring[i],ring[(i+1)%sides],tip);
                Array.Reverse(ring); Face(material,ring);
            }

            public void IrregularRock(string material, Vector3 center, Vector3 size)
            {
                int[] low=new int[7], high=new int[7];
                for(int i=0;i<7;i++)
                {
                    float a=i*Mathf.PI*2f/7f;
                    float r=.76f+((i*37)%5)*.055f;
                    low[i]=V(center+new Vector3(Mathf.Cos(a)*size.x*.5f*r,-size.y*.5f,Mathf.Sin(a)*size.z*.5f*r));
                    high[i]=V(center+new Vector3(Mathf.Cos(a+.18f)*size.x*.32f*(1f-r*.15f),size.y*.22f+Mathf.Sin(i*2.1f)*.08f,Mathf.Sin(a+.18f)*size.z*.32f));
                }
                int tip=V(center+new Vector3(.08f,size.y*.55f,-.05f));
                for(int i=0;i<7;i++) { Face(material,low[i],low[(i+1)%7],high[(i+1)%7],high[i]); Face(material,high[i],high[(i+1)%7],tip); }
                Array.Reverse(low); Face(material,low);
            }

            public override string ToString() => s.ToString();
        }
    }
}
#endif
