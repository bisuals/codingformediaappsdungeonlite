using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    [CreateAssetMenu(menuName = "NoOpArmy/Wise Feline/Factions Collection")]
    public class FactionsCollection : ScriptableObject
    {
        public List<FactionData> factions;
    }

    [Serializable]
    public class FactionData
    {
        public int id;
        public string factionName;
        public string description;
    }


}
