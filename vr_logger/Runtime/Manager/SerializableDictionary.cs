using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLogger
{
    /// <summary>
    /// Unity no serializa Dictionary por defecto.
    /// Esto permite serializar diccionarios en JSON y mostrarlos correctamente.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // Guardar datos del diccionario → listas antes de serializar
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        // Cargar datos después de deserializar
        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
                throw new Exception("La cantidad de keys y values no coincide en SerializableDictionary.");

            for (int i = 0; i < keys.Count; i++)
            {
                this[keys[i]] = values[i];
            }
        }
    }
}
