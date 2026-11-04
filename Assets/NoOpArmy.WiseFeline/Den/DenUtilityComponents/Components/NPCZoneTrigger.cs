using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    public class NPCZoneTrigger : MonoBehaviour
    {
        public Transform myNPC;

        private List<GameObject> agentsInZone;
        private INPCZoneMessageReceiver receiver;

        /// <summary>
        /// Gets all agents which are in our zone
        /// </summary>
        /// <returns></returns>
        public List<GameObject> GetAgents()
        {
            return agentsInZone;
        }

        private void Awake()
        {
            agentsInZone = new List<GameObject>();
            receiver = myNPC.GetComponent<INPCZoneMessageReceiver>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform == myNPC)
                return;

            myNPC.LookAt(other.transform);
            agentsInZone.Add(other.gameObject);
            receiver?.OnOtherEnteredZone(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform == myNPC)
                return;

            agentsInZone.Remove(other.gameObject);
            receiver?.OnOtherLeftZone(other.gameObject);
        }
    }

    public interface INPCZoneMessageReceiver
    {
        void OnOtherEnteredZone(GameObject other);
        void OnOtherLeftZone(GameObject other);
    }
}
