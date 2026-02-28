using UnityEngine;

namespace VRLogger.Trackers
{
    /// <summary>
    /// Componente Plug & Play para registrar localización de sonido.
    /// ALIMENTA LAS MÉTRICAS EN PYTHON: SoundLocalizationTimeS
    /// USO: Arrástralo AL MISMO GameObject que tiene el "AudioSource".
    /// </summary>
    [AddComponentMenu("VR Logger/Metrics/Audio Reaction Logger")]
    [RequireComponent(typeof(AudioSource))]
    public class AudioReactionLogger : MonoBehaviour
    {
        [Tooltip("ID de la Fuente de Audio (ej. Sonido_Explosion, Voz_NPC). Si está vacío usa el nombre del objeto.")]
        public string audioSourceId = "";

        private AudioSource _audioSource;
        private bool _isPlaying = false;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private string GetAudioId()
        {
            return string.IsNullOrEmpty(audioSourceId) ? gameObject.name : audioSourceId;
        }

        private void Update()
        {
            if (_audioSource == null) return;

            // Detectar el momento exacto en que empieza a sonar
            if (_audioSource.isPlaying && !_isPlaying)
            {
                _isPlaying = true;
                ReportAudioTriggered();
            }
            // Detectar cuando termina de sonar para el próximo trigger
            else if (!_audioSource.isPlaying && _isPlaying)
            {
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Expuesto por si se desea llamar a mano en lugar de usar Update automático.
        /// Registra en Python el instante en que el sonido empieza.
        /// </summary>
        public void ReportAudioTriggered()
        {
            LogAPI.LogAudioTriggered(GetAudioId());
            Debug.Log($"[AudioReactionLogger] 🔊 Audio Triggered: {GetAudioId()}");
        }
    }
}
