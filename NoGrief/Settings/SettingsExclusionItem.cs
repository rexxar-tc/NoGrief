namespace NoGriefPlugin.Settings
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [Serializable]
    public class ExclusionItem
    {
        private bool enabled;
        private long entityId;
        private int exclusionRadius;
        private string exclusionMessage;
        private bool transportAdd;
        private List<ulong> allowedPlayers;
        private List<long> allowedEntities;
        private bool allowAdmins;
        private string factionTag;

        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

        public long EntityId
        {
            get
            {
                return entityId;
            }
            set
            {
                entityId = value;
            }
        }

        public int ExclusionRadius
        {
            get
            {
                return exclusionRadius;
            }
            set
            {
                exclusionRadius = value;
            }
        }

        public string ExclusionMessage
        {
            get
            {
                return exclusionMessage;
            }
            set
            {
                exclusionMessage = value;
            }
        }

        public bool TransportAdd
        {
            get
            {
                return transportAdd;
            }
            set
            {
                transportAdd = value;
            }
        }

        public List<ulong> AllowedPlayers
        {
            get
            {
                return allowedPlayers;
            }
            set
            {
                allowedPlayers = value;
            }
        }

        public List<long> AllowedEntities
        {
            get
            {
                return allowedEntities;
            }
            set
            {
                allowedEntities = value;
            }
        }

        public string FactionTag
        {
            get
            {
                return factionTag;
            }
            set
            {
                factionTag = (value == null ? "" : value);
            }
        }

        public bool AllowAdmins
        {
            get
            {
                return allowAdmins;
            }
            set
            {
                allowAdmins = value;
            }
        }
    }
}
