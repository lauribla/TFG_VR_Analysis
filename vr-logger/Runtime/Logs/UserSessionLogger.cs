using UnityEngine;

namespace VRLogger
{
    public class UserSessionManager : MonoBehaviour
    {
        [Header("Mongo Config")]
        public string connectionString = "mongodb://localhost:27017";
        public string dbName = "VRLogsDB";
        public string collectionName = "events";
        public string userId = "U001"; // aquí puedes asignar ID dinámico

        void Start()
        {
            LoggerService.Init(connectionString, dbName, collectionName, userId);
        }
    }
}