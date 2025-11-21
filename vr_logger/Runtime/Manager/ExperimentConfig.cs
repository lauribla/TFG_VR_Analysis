using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace VRLogger
{
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance;

        private JObject jsonConfig;   // JSON completo

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

        void LoadConfig()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("experiment_config");

            if (jsonFile == null)
            {
                Debug.LogError("[ExperimentConfig] ❌ No se encontró Resources/experiment_config.json");
                return;
            }

            try
            {
                jsonConfig = JObject.Parse(jsonFile.text);

                Debug.Log("[ExperimentConfig] ✅ Config cargado correctamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExperimentConfig] ❌ Error parseando JSON: " + ex.Message);
            }
        }

        public async void SendConfigAsLog()
        {
            if (jsonConfig == null)
            {
                Debug.LogError("[ExperimentConfig] ❌ No hay config cargado para enviar.");
                return;
            }

            Debug.Log("[ExperimentConfig] 📤 Enviando config REAL tal cual a Mongo...");

            await LoggerService.LogEvent(
                "config",
                "experiment_config",
                null,
                jsonConfig
            );

            Debug.Log("[ExperimentConfig] ✅ Config enviado a Mongo.");
        }

        public JObject GetConfig()
        {
            return jsonConfig;
        }
    }
}
