using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace VRLogger
{
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance;

        public ExperimentConfigData config;

        // Ruta relativa dentro de Assets
        public string configPath = "vr_logger/experiment_config.json";

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            LoadConfig();
        }

        public void LoadConfig()
        {
            string fullPath = Path.Combine(Application.dataPath, configPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[ExperimentConfig] ❌ No se encontró el archivo de configuración en: {fullPath}");
                return;
            }

            string json = File.ReadAllText(fullPath);
            config = JsonConvert.DeserializeObject<ExperimentConfigData>(json);

            Debug.Log("[ExperimentConfig] Config loaded:\n" + json);
        }

        public void SaveConfig(string outputName = "experiment_config_runtime.json")
        {
            string fullPath = Path.Combine(Application.dataPath, "vr_logger", outputName);
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);

            File.WriteAllText(fullPath, json);

            Debug.Log("[ExperimentConfig] Config saved:\n" + json);
        }

        public async void SendConfigAsLog()
        {
            if (config == null)
            {
                Debug.LogError("[ExperimentConfig] ❌ No hay config cargado.");
                return;
            }

            Debug.Log("[ExperimentConfig] 📤 Enviando CONFIG a Mongo...");

            await LoggerService.LogEvent(
                "config",
                "experiment_config",
                null,
                config
            );
        }

    }
}
