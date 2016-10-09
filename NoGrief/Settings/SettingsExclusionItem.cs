using System;
using System.Collections.Generic;

namespace NoGriefPlugin.Settings
{
    [Serializable]
    public class ExclusionItem
    {
        private bool allowAdmins;
        private List<long> allowedEntities;
        private List<ulong> allowedPlayers;
        private bool enabled;
        private long entityId;
        private string exclusionMessage;
        private int exclusionRadius;
        private string factionTag;
        private bool transportAdd;

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public long EntityId
        {
            get { return entityId; }
            set { entityId = value; }
        }

        public int ExclusionRadius
        {
            get { return exclusionRadius; }
            set { exclusionRadius = value; }
        }

        public string ExclusionMessage
        {
            get { return exclusionMessage; }
            set { exclusionMessage = value; }
        }

        public bool TransportAdd
        {
            get { return transportAdd; }
            set { transportAdd = value; }
        }

        public List<ulong> AllowedPlayers
        {
            get { return allowedPlayers; }
            set { allowedPlayers = value; }
        }

        public List<long> AllowedEntities
        {
            get { return allowedEntities; }
            set { allowedEntities = value; }
        }

        public string FactionTag
        {
            get { return factionTag; }
            set { factionTag = value == null ? "" : value; }
        }

        public bool AllowAdmins
        {
            get { return allowAdmins; }
            set { allowAdmins = value; }
        }
    }
}