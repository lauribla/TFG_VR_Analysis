using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace VRLogger
{
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance;

        public dynamic config;

        void Awake()
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

        // ------------------------------------------------------------
        // 🚀 1) Cargar experiment_config REAL desde Resources
        // ------------------------------------------------------------
        void LoadConfig()
        {
            try
            {
                TextAsset jsonFile = Resources.Load<TextAsset>("experiment_config");

                if (jsonFile == null)
                {
                    Debug.LogError("[ExperimentConfig] ❌ No se encontró experiment_config.json en Resources/");
                    return;
                }

                config = JsonConvert.DeserializeObject(jsonFile.text);

                Debug.Log("[ExperimentConfig] ✅ Config cargado correctamente:");
                Debug.Log(jsonFile.text);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExperimentConfig] ❌ Error al cargar config: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // 🚀 2) Enviar config REAL a Mongo
        // ------------------------------------------------------------
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

            Debug.Log("[ExperimentConfig] ✅ CONFIG enviado correctamente");
        }
    }
}
