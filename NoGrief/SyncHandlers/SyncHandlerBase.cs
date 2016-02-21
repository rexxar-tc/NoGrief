using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Sandbox.Engine.Multiplayer;
using SEModAPIInternal.API.Common;
using NoGriefPlugin.Utility;

namespace NoGriefPlugin.CustomPacketHandlers
{
    public class SyncHandlerBase
    {
        public delegate void ReceivePacketStatic<T>(ref T packet, Object netManager) where T : struct;
        public delegate void ReceivePacketEntity<T>(Object instanceNetManager, ref T packet, Object masterNetManager) where T : struct;

        internal void RegisterCustomPacketHandler(Type syncType, string packetTypeName, PacketRegistrationType registrationType, MyTransportMessageEnum transportMsgType, MethodInfo handler)
        {
            Type packetType = syncType.GetNestedType(packetTypeName, BindingFlags.Public | BindingFlags.NonPublic);
            if (packetType == null)
            {
                //Logging.WriteLineAndConsole(string.Format("Packet type is null"));
                NoGrief.Log.Info("Packet type is null");
                return;
            }
            
            Type masterNetManagerType = SandboxGameAssemblyWrapper.Instance.GetAssemblyType("Sandbox.Game.Multiplayer","MySyncLayer");
            FieldInfo packetRegisteryHashSetField = masterNetManagerType.GetField("m_registrators", BindingFlags.NonPublic | BindingFlags.Static);
            if (packetRegisteryHashSetField != null)
            {
                Object packetRegisteryHashSetRaw = packetRegisteryHashSetField.GetValue(null);
                //HashSet<Object> packetRegisteryHashSet = General.ConvertHashSet(packetRegisteryHashSetRaw);
                HashSet<Object> packetRegisteryHashSet = packetRegisteryHashSetRaw as HashSet<object>;
                if (packetRegisteryHashSet == null || packetRegisteryHashSet.Count == 0)
                    return;

                Object matchedHandler = null;
                List<Object> matchedHandlerList = new List<object>();
                List<Type> messageTypes = new List<Type>();
                foreach (var entry in packetRegisteryHashSet)
                {
                    FieldInfo delegateField = entry.GetType().GetField("Factory");
                    FieldInfo msgTypeField = entry.GetType().GetField("MessageType");
                    MyTransportMessageEnum entryMsgType = (MyTransportMessageEnum)msgTypeField.GetValue(entry);

                    Type fieldType = delegateField.FieldType;
                    Type[] genericArgs = fieldType.GetGenericArguments();
                    Type[] messageTypeArgs = genericArgs[1].GetGenericArguments();
                    Type messageType = messageTypeArgs[0];
                    if (messageType == packetType && transportMsgType == entryMsgType)
                    {
                        matchedHandler = entry;
                        matchedHandlerList.Add(entry);
                    }

                    messageTypes.Add(messageType);
                }

                if (matchedHandlerList.Count > 1)
                {
                    //Logging.WriteLineAndConsole("Found more than 1 packet handler");
                    NoGrief.Log.Info("Found more than 1 packet handler");
                    return;
                }

                if (matchedHandler == null)
                {
                    //Logging.WriteLineAndConsole("Matched handler is null");
                    NoGrief.Log.Info("Matched handler is null");
                    return;
                }

                FieldInfo field = matchedHandler.GetType().GetField("Factory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                Object value = field.GetValue(matchedHandler);
                FieldInfo secondaryFlagsField = matchedHandler.GetType().GetField("MessageType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                Object secondaryFlags = secondaryFlagsField.GetValue(matchedHandler);
                MulticastDelegate action = (MulticastDelegate)value;
                object target = action.Target;
                FieldInfo field2 = target.GetType().GetField("factory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                Object value2 = field2.GetValue(target);
                MulticastDelegate action2 = (MulticastDelegate)value2;
                object target2 = action2.Target;

                string field3Name = "callback";
                string flagsFieldName = "permissions";
                string serializerFieldName = "serializer";

                FieldInfo field3 = target2.GetType().GetField(field3Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                Object value3 = field3.GetValue(target2);
                MulticastDelegate action3 = (MulticastDelegate)value3;

                FieldInfo flagsField = target2.GetType().GetField(flagsFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                Object flagsValue = flagsField.GetValue(target2);
                FieldInfo serializerField = target2.GetType().GetField(serializerFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                Object serializerValue = serializerField.GetValue(target2);

                FieldInfo methodBaseField = action3.GetType().GetField("_methodBase", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                FieldInfo methodPtrField = action3.GetType().GetField("_methodPtr", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                FieldInfo methodPtrAuxField = action3.GetType().GetField("_methodPtrAux", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                Delegate handlerAction = CreatePacketHandlerDelegate(registrationType, packetType, handler);

                //Remove the old handler from the registry
                MethodInfo removeMethod = packetRegisteryHashSetRaw.GetType().GetMethod("Remove");
                removeMethod.Invoke(packetRegisteryHashSetRaw, new object[] { matchedHandler });

                //Update the handler delegate with our new method info
                methodBaseField.SetValue(action3, handlerAction.Method);
                methodPtrField.SetValue(action3, methodPtrField.GetValue(handlerAction));
                methodPtrAuxField.SetValue(action3, methodPtrAuxField.GetValue(handlerAction));

                if (registrationType == PacketRegistrationType.Instance)
                {
                    MethodInfo registerEntityMessage = masterNetManagerType.GetMethod("RegisterEntityMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    MethodInfo registerMethod = registerEntityMessage.MakeGenericMethod(syncType, packetType);
                    registerMethod.Invoke(null, new object[] { action3, flagsValue, secondaryFlags, serializerValue });
                }
                else if (registrationType == PacketRegistrationType.Static)
                {
                    MethodInfo registerMessage = masterNetManagerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Single(x =>
                    {
                        return x.Name == "RegisterMessage" && x.GetParameters().Length == 4 && (!(x.GetParameters().FirstOrDefault(y => y.ParameterType.Name.Contains("Time")) != null));
                    });

                    MethodInfo registerMethod = registerMessage.MakeGenericMethod(packetType);
                    registerMethod.Invoke(null, new object[] { action3, flagsValue, secondaryFlags, serializerValue });
                }
            }
        }

        private static Delegate CreatePacketHandlerDelegate(PacketRegistrationType registrationType, Type packetType, MethodInfo handler)
        {
            try
            {
                Type delegateType = null;
                switch (registrationType)
                {
                    case PacketRegistrationType.Static:
                        delegateType = typeof(ReceivePacketStatic<>);
                        break;
                    case PacketRegistrationType.Instance:
                        delegateType = typeof(ReceivePacketEntity<>);
                        break;
                    //                    case PacketRegistrationType.Timespan:
                    //                        delegateType = typeof(ReceivePacketTimespan<>);
                    //                        break;
                    default:
                        return null;
                }

                delegateType = delegateType.MakeGenericType(packetType);
                handler = handler.MakeGenericMethod(packetType);
                Delegate action = Delegate.CreateDelegate(delegateType, handler);
                return action;
            }
            catch (Exception ex)
            {
                //Logging.WriteLineAndConsole(string.Format("Error: {0}", ex.ToString()));
                NoGrief.Log.Error(ex,"Error");
                return null;
            }
        }
    }
}