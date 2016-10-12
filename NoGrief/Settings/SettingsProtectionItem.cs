using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Collections;

namespace NoGriefPlugin.Settings
{
    [Serializable]
    public class SettingsProtectionItem
    {
        private bool _enabled;
        private float _radius;
        private long _entityId;
        private bool _stopDamage;
        private bool _stopPaint;
        private bool _stopBuild;
        private bool _stopRemoveBlock;
        private bool _stopDeleteGrid;
        private bool _adminExempt;
        private bool _ownerExempt;
        private bool _stopPlayerDamage;

        public bool Enabled
        {
            get {return _enabled; }
            set { _enabled = value; }
        }

        public float Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public long EntityId
        {
            get { return _entityId; }
            set { _entityId = value; }
        }

        public bool StopDamage
        {
            get { return _stopDamage; }
            set { _stopDamage = value; }
        }

        public bool StopPaint
        {
            get { return _stopPaint; }
            set { _stopPaint = value; }
        }

        public bool StopBuild
        {
            get { return _stopBuild; }
            set { _stopBuild = value; }
        }

        public bool StopRemoveBlock
        {
            get { return _stopRemoveBlock; }
            set
            {
                if(value && !StopDamage)
                    throw new ArgumentException("StopDamage must be enabled to use StopRemoveBlock");

                _stopRemoveBlock = value;
            }
        }

        public bool StopDeleteGrid
        {
            get { return _stopDeleteGrid; }
            set { _stopDeleteGrid = value; }
        }

        public bool AdminExempt
        {
            get { return _adminExempt; }
            set { _adminExempt = value; }
        }

        public bool OwnerExempt
        {
            get { return _ownerExempt; }
            set { _ownerExempt = value; }
        }

        public bool StopPlayerDamage
        {
            get { return _stopPlayerDamage; }
            set { _stopPlayerDamage = value; }
        }

        [XmlIgnore]
        [Browsable(false)]
        public readonly MyConcurrentHashSet<long> ContainsEntities = new MyConcurrentHashSet<long>();
    }
}
