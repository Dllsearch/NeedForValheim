using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public static class BuildVehicleShader
{
    public static void Build()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-nfvu2-output");
        if (index < 0 || index + 1 >= args.Length || !Path.IsPathRooted(args[index + 1]))
            throw new ArgumentException("Supply an absolute -nfvu2-output directory.");
        string output = Path.GetFullPath(args[index + 1]);
        const string asset = "Assets/NFVU2CarPaint.shader";
        AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(asset);
        if (shader == null) throw new InvalidOperationException("Car paint shader is missing.");
        foreach (var message in ShaderUtil.GetShaderMessages(shader))
            if (message.severity == ShaderCompilerMessageSeverity.Error)
                throw new InvalidOperationException(message.message);
        Directory.CreateDirectory(output);
        var bundle = new AssetBundleBuild {
            assetBundleName = "nfvu2-car-shaders",
            assetNames = new[] { asset }
        };
        var manifest = BuildPipeline.BuildAssetBundles(output, new[] { bundle },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64);
        if (manifest == null) throw new InvalidOperationException("Shader bundle build failed.");
        if (Array.IndexOf(args, "-nfvu2-preview") >= 0) RenderPreview(output);
        Debug.Log("NFVU2 car shader bundle built: " + output);
    }

    static void RenderPreview(string output)
    {
        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "nfvu2-car-shaders"));
        if (bundle == null) throw new InvalidOperationException("Cannot load generated bundle.");
        var shader = bundle.LoadAsset<Shader>("Assets/NFVU2CarPaint.shader");
        if (shader == null || !shader.isSupported)
            throw new InvalidOperationException("Bundled shader is unavailable on the current graphics device.");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.25f, .25f, .25f);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var material = new Material(shader);
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        material.SetColor("_Color", new Color(.04f, .2f, .65f));
        material.SetColor("_ShiftColor", new Color(.8f, .03f, .2f));
        material.SetFloat("_Metallic", .2f);
        var light = new GameObject("Preview light").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.5f;
        light.transform.rotation = Quaternion.Euler(35, -30, 0);
        var camera = new GameObject("Preview camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -2);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .035f, .035f);
        camera.orthographic = true; camera.orthographicSize = .65f;
        var target = new RenderTexture(320, 320, 24);
        camera.targetTexture = target;
        var previous = RenderTexture.active;
        Color32[] baseline = null;
        bool allShifted = true;
        try {
            for (int mode = 0; mode < 3; mode++) {
                material.SetFloat("_FinishMode", mode);
                camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(320, 320, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 320, 320), 0, 0); image.Apply();
                var pixels = image.GetPixels32();
                if (mode == 0) baseline = pixels;
                else {
                    int changed = 0;
                    for (int i = 0; i < pixels.Length; i++)
                        if (Math.Abs(pixels[i].r - baseline[i].r) + Math.Abs(pixels[i].g - baseline[i].g) + Math.Abs(pixels[i].b - baseline[i].b) > 6) changed++;
                    if (changed < 100) allShifted = false;
                    Debug.Log("NFVU2 bundled finish " + mode + ": " + changed + " shifted pixels.");
                }
                File.WriteAllBytes(Path.Combine(output, "finish-" + mode + ".png"), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            if (!allShifted) throw new InvalidOperationException("A finish has no measurable rendered colour shift; inspect finish PNGs.");
            material.SetFloat("_CompositePaint", 1);
            material.SetColor("_Color", Color.white);
            material.SetColor("_PaintBaseColor", new Color(.04f, .2f, .65f));
            var composite = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try {
                material.SetTexture("_MainTex", composite);
                for (int uncovered = 0; uncovered <= 1; uncovered++) {
                    composite.SetPixel(0, 0, uncovered == 0
                        ? new Color(.7f, .1f, .2f, 0) : new Color(.04f, .2f, .65f, 1));
                    composite.Apply();
                    Color32[] ordinary = null;
                    for (int mode = 0; mode <= 2; mode++) {
                        material.SetFloat("_FinishMode", mode);
                        camera.Render(); RenderTexture.active = target;
                        var image = new Texture2D(320, 320, TextureFormat.RGB24, false);
                        try {
                            image.ReadPixels(new Rect(0, 0, 320, 320), 0, 0); image.Apply();
                            var pixels = image.GetPixels32();
                            if (mode == 0) ordinary = pixels;
                            else {
                                int changed = 0;
                                for (int i = 0; i < pixels.Length; i++)
                                    if (Math.Abs(pixels[i].r - ordinary[i].r) + Math.Abs(pixels[i].g - ordinary[i].g) + Math.Abs(pixels[i].b - ordinary[i].b) > 6) changed++;
                                if (uncovered == 0 ? changed != 0 : changed < 100)
                                    throw new InvalidOperationException("Rendered vinyl paint mask failed: uncovered=" + uncovered + ", mode=" + mode + ", changed=" + changed);
                                Debug.Log("NFVU2 vinyl mask: uncovered=" + uncovered + ", mode=" + mode + ", changed=" + changed);
                            }
                            File.WriteAllBytes(Path.Combine(output, "vinyl-mask-" + uncovered + "-finish-" + mode + ".png"), image.EncodeToPNG());
                        } finally { UnityEngine.Object.DestroyImmediate(image); }
                    }
                }
            } finally { material.SetTexture("_MainTex", null); UnityEngine.Object.DestroyImmediate(composite); }
        } finally {
            RenderTexture.active = previous; camera.targetTexture = null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(material); bundle.Unload(false);
        }
    }
}
