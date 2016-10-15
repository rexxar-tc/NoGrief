using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NoGriefPlugin.Utility;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace NoGriefPlugin.ProcessHandlers
{
    public class ProcessZoneBoundaries : ProcessHandlerBase
    {
        public ProcessZoneBoundaries()
        {
            //do refelction unit tests and cache some stuff
            _myProjectilesType = Utilities.FindTypeInAllAssemblies("Sandbox.Game.Weapons.MyProjectiles");
            if (_myProjectilesType == null)
                throw new TypeAccessException("Can't find type for MyProjectiles!");
            //this mess of reflection gets the internal pool of projectiles so we can stop them later
            projPool = _myProjectilesType.GetField("m_projectiles", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            if (projPool == null)
                throw new FieldAccessException("Can't get m_projectile");
            //this field is a HashSetReader, we need this instead of the MyObjectsPool
            var projActive = projPool.GetType().GetProperty("Active", BindingFlags.Instance | BindingFlags.Public);
            if (projActive == null)
                throw new FieldAccessException("Can't get Active");
            var projectiles = projActive.GetValue(projPool);

            if (projectiles == null)
                throw new FieldAccessException("Can't get value for m_projectiles!");

            var projectileType = Utilities.FindTypeInAllAssemblies("Sandbox.Game.Weapons.MyProjectile");
            if (projectileType == null)
                throw new TypeAccessException("Can't find type for MyProjectile!");

            FieldInfo posField = projectileType.GetField("m_position", BindingFlags.NonPublic | BindingFlags.Instance);
            if (posField == null)
                throw new FieldAccessException("m_position");

            FieldInfo state = projectileType.GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);
            if (state == null)
                throw new FieldAccessException("m_state");

            NoGrief.Log.Info("Projectile management passed reflection unit test");
        }
        
        private readonly Type _myProjectilesType;
        private readonly object projPool;

        public override int GetUpdateResolution()
        {
            return 100;
        }

        public override void Handle()
        {
            if (!PluginSettings.Instance.ProtectionZonesEnabled && !PluginSettings.Instance.ExclusionZonesEnabled)
                return;
            
            //the type is MyHashSetReader<MyProjectile>, but MyProjectile is private
            //cast to generic IEnumberable so we can get around this
            var projectiles = projPool.GetType().GetProperty("Active", BindingFlags.Instance | BindingFlags.Public).GetValue(projPool) as IEnumerable;
            
            //thread safety
            var projArray = projectiles.Cast<object>().ToArray();
            if (projArray == null)
                throw new Exception("Try the other one");

            var entities = MyEntities.GetEntities().ToArray();
            if (entities.Length == 0)
            {
                NoGrief.Log.Info("Failed to get entities in zone boundary update. Skipping update");
                return;
            }

            if (PluginSettings.Instance.ProtectionZonesEnabled)
            {
                foreach (var item in PluginSettings.Instance.ProtectionItems)
                {
                    if (!item.Enabled)
                        continue;

                    MyEntity ent;
                    if (!MyEntities.TryGetEntityById(item.EntityId, out ent))
                        continue;

                    if (ent.Closed || ent.MarkedForClose || ent.Physics == null)
                        continue;

                    if (!Vector3.IsZero(ent.Physics.LinearVelocity) || !Vector3.IsZero(ent.Physics.AngularVelocity))
                        continue;

                    var sphere = new BoundingSphereD(ent.Center(), item.Radius);
                    var outerSphere = new BoundingSphereD(ent.Center(), item.Radius + 50);

                    //destroy any missiles crossing the zone boundary
                    foreach (var entity in entities)
                    {
                        if (entity.Closed || entity.MarkedForClose || entity.Physics == null)
                            continue;

                        if (!(entity is MyAmmoBase))
                            continue;

                        if (outerSphere.Contains(entity.Center()) != ContainmentType.Disjoint && sphere.Contains(entity.Center()) == ContainmentType.Disjoint)
                            Wrapper.BeginGameAction(() => ((IMyDestroyableObject)entity).DoDamage(100, MyStringHash.GetOrCompute("Explosion"), true));
                    }

                    //delete any bullets that are crossing the zone boundary
                    foreach (object obj in projArray)
                    {
                        FieldInfo posField = obj.GetType().GetField("m_position", BindingFlags.NonPublic | BindingFlags.Instance);

                        var pos = (Vector3D)posField.GetValue(obj);
                        if (outerSphere.Contains(pos) != ContainmentType.Disjoint && sphere.Contains(pos) == ContainmentType.Disjoint)
                        {
                            //obj.GetType().GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(obj, null);
                            FieldInfo state = obj.GetType().GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);

                            //set state to KILLED_AND_DRAWN(2) to trick the game into removing the bullet on the next update
                            //this doesn't sync :(
                            state.SetValue(obj, (byte)2);
                        }
                    }
                }
            }
            if (PluginSettings.Instance.ExclusionZonesEnabled)
            {
                foreach (var item in PluginSettings.Instance.ExclusionItems)
                {
                    if (!item.Enabled)
                        continue;

                    MyEntity ent;
                    if (!MyEntities.TryGetEntityById(item.EntityId, out ent))
                        continue;

                    if (ent.Closed || ent.MarkedForClose || ent.Physics == null)
                        continue;

                    if (!Vector3.IsZero(ent.Physics.LinearVelocity) || !Vector3.IsZero(ent.Physics.AngularVelocity))
                        continue;

                    var sphere = new BoundingSphereD(ent.Center(), item.Radius);
                    var outerSphere = new BoundingSphereD(ent.Center(), item.Radius + 50);

                    //destroy any missiles crossing the zone boundary
                    foreach (var entity in entities)
                    {
                        if (entity.Closed || entity.MarkedForClose || entity.Physics == null)
                            continue;

                        if (!(entity is MyAmmoBase))
                            continue;

                        if (outerSphere.Contains(entity.Center()) != ContainmentType.Disjoint && sphere.Contains(entity.Center()) == ContainmentType.Disjoint)
                            Wrapper.BeginGameAction(() => ((IMyDestroyableObject)entity).DoDamage(100, MyStringHash.GetOrCompute("Explosion"), true));
                    }

                    //delete any bullets that are crossing the zone boundary
                    foreach (object obj in projArray)
                    {
                        FieldInfo posField = obj.GetType().GetField("m_position", BindingFlags.NonPublic | BindingFlags.Instance);

                        var pos = (Vector3D)posField.GetValue(obj);
                        if (outerSphere.Contains(pos) != ContainmentType.Disjoint && sphere.Contains(pos) == ContainmentType.Disjoint)
                        {
                            //obj.GetType().GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(obj, null);
                            FieldInfo state = obj.GetType().GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);

                            //set state to KILLED_AND_DRAWN(2) to trick the game into removing the bullet on the next update
                            //this doesn't sync :(
                            state.SetValue(obj, (byte)2);
                        }
                    }
                }
            }
        }
    }
}
