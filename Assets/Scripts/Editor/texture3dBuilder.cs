using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class texture3dBuilder : EditorWindow
{
    //Given a folder filled with images, it turns them into a unity 3D texture.
    //Images are sorted by file name.
    [MenuItem("Tools/Create Texture3D")]
    public static void Create()
    {
        string path = EditorUtility.OpenFolderPanel("Select folder", "", "");

        if (path == null || path == "") { return; }

        string[] files = Directory.GetFiles(path);
        List<string> imagePaths = new List<string>();

        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].EndsWith(".png"))
            {
                imagePaths.Add(files[i]);
            }
        }

        if (imagePaths.Count == 0) { Debug.LogWarning("No valid images found."); return; }

        imagePaths.Sort();

        List<Texture2D> images = new();
        int size = 0;
        TextureFormat format = TextureFormat.RGBA32;



        for (int i = 0; i < imagePaths.Count; i++)
        {
            byte[] data = File.ReadAllBytes(imagePaths[i]);
            if (data == null || data.Length == 0) { Debug.LogWarning("Could not open image: " + imagePaths[i]); return; }

            Texture2D img = new Texture2D(0, 0);
            if (!ImageConversion.LoadImage(img, data)) { Debug.LogWarning("Could not load image: " + imagePaths[i]); return; }

            if (img.width != img.height) { Debug.LogWarning("Different width and height: " + imagePaths[i]); return; }

            if (i == 0)
            {
                size = img.width;
                format = img.format;
            }
            else if (img.width != size) { Debug.LogWarning("Different image dimension: " + imagePaths[i]); return; }
            else if (img.format != format) { Debug.LogWarning("Different image format: " + imagePaths[i]); return; }

            images.Add(img);
        }

        if (images.Count != size) { Debug.LogWarning("Wrong amount of images: " + images.Count + "/" + size); return; }


        TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        Texture3D texture = new(size, size, size, format, false);
        texture.wrapMode = wrapMode;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    texture.SetPixel(x, y, z, images[z].GetPixel(x, y));
                }
            }
        }
        
        texture.Apply();

        AssetDatabase.CreateAsset(texture, "Assets/Textures/new3DTexture.asset");
    }
}
