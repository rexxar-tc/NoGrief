using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoGrief.Settings
{
    [Serializable]
    public class AdminItem
    {
        private bool enabled;
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

        private long entityId;
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
    }
}
