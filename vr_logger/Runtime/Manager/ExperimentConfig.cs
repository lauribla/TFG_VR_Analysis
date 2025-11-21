using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace VRLogger
{
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance;

        public ExperimentConfigData config;   // <-- SIN dynamic

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
        // CARGAR JSON REAL DESDE Resources
        // ------------------------------------------------------------
        void LoadConfig()
        {
            try
            {
                TextAsset jsonFile = Resources.Load<TextAsset>("experiment_config");

                if (jsonFile == null)
                {
                    Debug.LogError("[ExperimentConfig] ❌ NO se encontró Resources/experiment_config.json");
                    return;
                }

                config = JsonConvert.DeserializeObject<ExperimentConfigData>(jsonFile.text);

                Debug.Log("[ExperimentConfig] ✅ Config cargado correctamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExperimentConfig] ❌ Error cargando config: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // ENVIAR CONFIG A MONGO
        // ------------------------------------------------------------
        public async void SendConfigAsLog()
        {
            if (config == null)
            {
                Debug.LogError("[ExperimentConfig] ❌ No hay config cargado para enviar.");
                return;
            }

            Debug.Log("[ExperimentConfig] 📤 Enviando config REAL a Mongo...");

            await LoggerService.LogEvent(
                "config",
                "experiment_config",
                null,
                config  // <-- ahora es serializable sin dynamic
            );

            Debug.Log("[ExperimentConfig] ✅ Config enviado a Mongo");
        }
    }
}
