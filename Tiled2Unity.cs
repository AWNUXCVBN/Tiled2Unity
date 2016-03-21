using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
public class Tiled2Unity : EditorWindow
{
    private XmlDocument tmxXml;
    Map map;
    private struct Layer
    {
        public string name;
        public int width;
        public int height;
        public string encoding;
        public int[] data;
    }

    private struct Tileset
    {
        public int firstgid;
        public string name;
        public int tilewidth;
        public int tileheight;
        public int spacing;
        public int margin;
        public int tilecount;
        public int columns;
        public string source;
        public float width;
        public float height;
    }

    private struct Map
    {
        public string version;
        public string orientation;
        public string renderorder;
        public int width;
        public int height;
        public int tilewidth;
        public int tileheight;
        public int nextobjectid;
        public List<Tileset> tilesets;
        public List<Layer> layers;
    }

    [MenuItem("GameObject/Tiled2Unity")]
    static void AddWindow()
    {
        Rect wr = new Rect(0, 0, 500, 500);
        Tiled2Unity t2u = (Tiled2Unity)EditorWindow.GetWindowWithRect(typeof(Tiled2Unity), wr, true, "Tiled2Unity");
        
    }
     
    static string tmxPath = null;
    static string[] texturesPath;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    void OnGUI()
    {
        if (GUILayout.Button("选择tmx文件路径"))
        {
            tmxPath = EditorUtility.OpenFilePanel("选择tmx文件", "", "tmx"); 
            if (tmxPath != null&&tmxPath !="")
            {
                LoadTmx(tmxPath);
            } 
        }
        GUILayout.Label("tmx文件路径：" + tmxPath, EditorStyles.boldLabel);
    }

    void LoadTmx(string path)
    {
        string s = File.ReadAllText(path, Encoding.UTF8); 
        tmxXml = new XmlDocument();
        tmxXml.LoadXml(s);
        ResolveTmx(tmxXml);
        CreatMMM();
    }

    void ResolveTmx(XmlDocument xml)
    {
        map = new Map();
        if (xml.DocumentElement.Name == "map")
        {
            map.version = xml.DocumentElement.Attributes["version"].Value;
            map.orientation = xml.DocumentElement.Attributes["orientation"].Value;
            map.renderorder = xml.DocumentElement.Attributes["renderorder"].Value;
            map.width = ToInt(xml.DocumentElement.Attributes["width"].Value);
            map.height = ToInt(xml.DocumentElement.Attributes["height"].Value);
            map.tilewidth = ToInt(xml.DocumentElement.Attributes["tilewidth"].Value);
            map.tileheight = ToInt(xml.DocumentElement.Attributes["tileheight"].Value);
            map.nextobjectid = ToInt(xml.DocumentElement.Attributes["nextobjectid"].Value);

            XmlNodeList tilesetsNodes = xml.DocumentElement.SelectNodes("tileset");
            XmlNodeList layersNodes = xml.DocumentElement.SelectNodes("layer");

            XmlNode subNode = null;
            XmlNode subSubNode = null;

            map.tilesets = new List<Tileset>();
            map.layers = new List<Layer>();

            Debug.Log(tilesetsNodes.Count);

            for (int i = 0; i < tilesetsNodes.Count; i++)
            {
                subNode = tilesetsNodes[i];
                Tileset tileset = new Tileset();

                tileset.firstgid = ToInt(subNode.Attributes["firstgid"].Value);
                tileset.name = subNode.Attributes["name"].Value;
                tileset.tilewidth = ToInt(subNode.Attributes["tilewidth"].Value);
                tileset.tileheight = ToInt(subNode.Attributes["tileheight"].Value);

                if (subNode.Attributes["spacing"] != null)
                {
                    tileset.spacing = ToInt(subNode.Attributes["spacing"].Value);
                }
                else
                {
                    tileset.spacing = 0;
                }

                if (subNode.Attributes["margin"] != null)
                {
                    tileset.margin = ToInt(subNode.Attributes["margin"].Value);
                }
                else
                {
                    tileset.margin = 0;
                }

                if (subNode.Attributes["tilecount"] != null)
                {
                    tileset.tilecount = ToInt(subNode.Attributes["tilecount"].Value);
                }
                else
                {
                    tileset.tilecount = 0;
                }

                subSubNode = subNode.SelectSingleNode("image");
                tileset.source = subSubNode.Attributes["source"].Value;
                tileset.width = ToInt(subSubNode.Attributes["width"].Value);
                tileset.height = ToInt(subSubNode.Attributes["height"].Value);

                map.tilesets.Add(tileset);

            }


            for (int i = 0; i < layersNodes.Count; i++)
            {
                subNode = layersNodes[i];

                Layer layer = new Layer();
                layer.name = subNode.Attributes["name"].Value;
                layer.width = ToInt(subNode.Attributes["width"].Value);
                layer.height = ToInt(subNode.Attributes["height"].Value);

                XmlNode dataNode = subNode.SelectSingleNode("data");
                layer.encoding = dataNode.Attributes["encoding"].Value;

                layer.data = new int[layer.width * layer.height]; 
                string[] s = dataNode.InnerText.Split(',');

                for (int j = 0; j < layer.data.Length; j++)
                {
                    layer.data[j] = ToInt(s[j]);
                }
                map.layers.Add(layer);
            }
        }
        Debug.Log(map.layers[0].data[0].ToString());
    }

    void CreatMMM()  
    {
        GameObject go = Selection.activeGameObject;

        if (go.GetComponent<MeshFilter>() != null)
        {
            DestroyImmediate(go.GetComponent<MeshFilter>()); 
        }

        meshFilter = go.AddComponent<MeshFilter>();
        mesh = new Mesh();

        if (go.GetComponent<MeshRenderer>() != null)
        {
            DestroyImmediate(go.GetComponent<MeshRenderer>()); 
        }

        meshRenderer = go.AddComponent<MeshRenderer>();
        Material[] ms = new Material[map.tilesets.Count];
        for(int i =0;i<map.tilesets.Count;i++)
        {
            Material m = new Material(Shader.Find("Unlit/Transparent Cutout"));
            m.name = map.tilesets[i].name;
            m.mainTexture = (Texture2D)Resources.Load(map.tilesets[i].name,typeof(Texture2D));
            ms[i] = m; 
        }
        meshRenderer.materials =ms;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        Dictionary<int, List<int>> submeshesTriangles = new Dictionary<int, List<int>>();
          
        for(int i=0;i<map.layers.Count;i++)
        {
            Layer layer = map.layers[i];

            for (int row = 0; row <layer.height; row++)
            {
                for (int col = 0; col < layer.width; col++)
                {
                    int gid = layer.data[row * col];
                    if(gid>0)
                    {

                    }
                }
            }
        }

    } 

    int ToInt(string s)
    {
        return Convert.ToInt32(s);
    }
}