using UnityEditor;
using UnityEngine;

public static class FixFaceMaterials
{
    [MenuItem("Tools/Fix Face Materials")]
    private static void Fix()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        string[] guids = AssetDatabase.FindAssets("Face_ t:Material", new[] { "Assets/Characters/Materials" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // Leer la textura ANTES de cambiar el shader
            Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;

            Undo.RecordObject(mat, "Fix Face Material");
            mat.shader = lit;

            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);

            // Opaco + Alpha Clipping
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

            EditorUtility.SetDirty(mat);
            Debug.Log("Arreglado: " + path + (tex == null ? "  (sin textura en _MainTex!)" : ""));
        }

        AssetDatabase.SaveAssets();
    }
}