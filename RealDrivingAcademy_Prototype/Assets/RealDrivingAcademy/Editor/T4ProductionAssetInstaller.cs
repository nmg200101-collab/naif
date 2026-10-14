#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace RealDrivingAcademy.EditorTools
{
    public static class T4ProductionAssetInstaller
    {
        public const string ModelUrl =
            "https://raw.githubusercontent.com/JaronKBragg7337/spectral-gt-rs/main/spectral_gt_rs_game_ready.glb";

        public const string ModelDirectory =
            "Assets/RealDrivingAcademy/Art/Vehicles/SpectralGT";

        public const string ModelAssetPath =
            ModelDirectory + "/spectral_gt_rs_game_ready.glb";

        public const long MinimumExpectedBytes = 4_000_000;

        [MenuItem("Real Driving Academy/T4/Download Production Car")]
        public static void DownloadProductionCar()
        {
            EnsureInstalled(true);
        }

        public static bool EnsureInstalled(bool showDialog = false)
        {
            string absolutePath = Path.GetFullPath(ModelAssetPath);
            string absoluteDirectory = Path.GetDirectoryName(absolutePath);

            if (File.Exists(absolutePath))
            {
                var info = new FileInfo(absolutePath);
                if (info.Length >= MinimumExpectedBytes)
                {
                    AssetDatabase.ImportAsset(
                        ModelAssetPath,
                        ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                    if (showDialog)
                        EditorUtility.DisplayDialog(
                            "RDA T4",
                            "Production car asset is already installed and imported.",
                            "OK");

                    return AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) != null;
                }

                File.Delete(absolutePath);
            }

            Directory.CreateDirectory(absoluteDirectory);

            string tempPath = absolutePath + ".download";

            try
            {
                EditorUtility.DisplayProgressBar(
                    "RDA T4",
                    "Downloading CC0 Spectral GT RS production car...",
                    0.35f);

                using (var client = new WebClient())
                {
                    client.Headers.Add(
                        "User-Agent",
                        "RealDrivingAcademy-UnityEditor");

                    client.DownloadFile(ModelUrl, tempPath);
                }

                var downloaded = new FileInfo(tempPath);
                if (!downloaded.Exists || downloaded.Length < MinimumExpectedBytes)
                    throw new InvalidOperationException(
                        "Downloaded GLB is incomplete. Bytes: " +
                        (downloaded.Exists ? downloaded.Length : 0));

                if (File.Exists(absolutePath))
                    File.Delete(absolutePath);

                File.Move(tempPath, absolutePath);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(
                    ModelAssetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
                if (prefab == null)
                    throw new InvalidOperationException(
                        "The GLB downloaded successfully but Unity did not import it as a GameObject. " +
                        "Verify the UnityGLTF package resolved correctly.");

                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "RDA T4",
                        "Production car downloaded and imported successfully.",
                        "OK");

                Debug.Log(
                    "RDA T4 production asset installed: " +
                    ModelAssetPath);

                return true;
            }
            catch (Exception ex)
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                Debug.LogError(
                    "RDA T4 production asset download/import failed: " +
                    ex);

                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "RDA T4",
                        "Production car download/import failed.\n\n" + ex.Message,
                        "OK");

                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
#endif
