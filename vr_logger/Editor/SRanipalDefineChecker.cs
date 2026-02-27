using UnityEditor;
using System.Linq;

namespace VRLogger.Editor
{
    [InitializeOnLoad]
    public class SRanipalDefineChecker
    {
        private const string DEFINE_SYMBOL = "USE_SRANIPAL";

        static SRanipalDefineChecker()
        {
            CheckAndDefineSRanipal();
        }

        private static void CheckAndDefineSRanipal()
        {
            // Verificamos si existe el namespace de SRanipal en los assemblies
            bool hasSRanipal = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Any(t => t.FullName != null && t.FullName.Contains("ViveSR.anipal.Eye"));

            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

            var allDefines = definesString.Split(';').ToList();
            bool hasDefine = allDefines.Contains(DEFINE_SYMBOL);

            if (hasSRanipal && !hasDefine)
            {
                allDefines.Add(DEFINE_SYMBOL);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", allDefines.ToArray()));
                
                UnityEngine.Debug.Log($"[VRLogger] SDK Vive SRanipal detectado. Añadido automáticamente símbolo '{DEFINE_SYMBOL}'.");
            }
            else if (!hasSRanipal && hasDefine)
            {
                allDefines.Remove(DEFINE_SYMBOL);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", allDefines.ToArray()));
                
                UnityEngine.Debug.Log($"[VRLogger] SDK Vive SRanipal no detectado. Eliminado símbolo '{DEFINE_SYMBOL}'.");
            }
        }
    }
}
