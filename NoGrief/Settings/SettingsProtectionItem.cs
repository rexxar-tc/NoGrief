namespace NoGriefPlugin.Settings
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [Serializable]
    public class ProtectionItem
    {
        private bool enabled;
        private long entityId;
        private int protectionRadius;
        private int maxGridCount;
        private int maxGridSize;

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

        public int ProtectionRadius
        {
            get
            {
                return protectionRadius;
            }
            set
            {
                protectionRadius = value;
            }
        }

        public int MaxGridCount
        {
            get
            {
                return maxGridCount;
            }
            set
            {
                maxGridCount = value;
            }
        }

        public int MaxGridSize
        {
            get
            {
                return maxGridSize;
            }
            set
            {
                maxGridSize = value;
            }
        }
    }
}
