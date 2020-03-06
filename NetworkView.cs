using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using GServer.Containers;
using GServer.Messages;

namespace GServer
{
    public class NetView
    {
        private static int countOfViews;
        private static readonly string syncMethodPrefix = "syncObject@";

        private Dictionary<string, InvokeHelper> methods = new Dictionary<string, InvokeHelper>();
        private Dictionary<string, Dictionary<string, InfoHelper>> properties = new Dictionary<string, Dictionary<string, InfoHelper>>();
        private Dictionary<string, IMarshalable> methodsArguments = new Dictionary<string, IMarshalable>();
        private Dictionary<string, int> countOfClasses = new Dictionary<string, int>();
        private Dictionary<int, int> hashToNum = new Dictionary<int, int>();
        private Dictionary<string, Object> stringToObject = new Dictionary<string, Object>();
        private Dictionary<string, int> cache = new Dictionary<string, int>();
        private NetworkController netCon = NetworkController.Instance;
        private float syncPeriod;
        private int hash;
        private Thread syncRPCThread;
        private string getCacheKeyName(Object c, string key)
        {
            return key + "@" + getUniqueClassString(c);
        }
        public NetView(float syncPeriod = 1f) { this.syncPeriod = syncPeriod; hash = ++countOfViews; }

        public void InitInvoke(params Object[] classes)
        {
            var invokeSystemType = new InvokeAttribute().GetType();

            foreach (var c in classes)
            {
                if (!c.GetType().IsClass) continue;

                int num;
                string key = c.ToString();
                if (countOfClasses.TryGetValue(key, out num))
                {
                    num = num + 1;
                    countOfClasses.Remove(key);
                }
                else
                {
                    num = 1;
                }
                countOfClasses.Add(key, num);
                hashToNum.Add(c.GetHashCode(), num);
                stringToObject.Add(getUniqueClassString(c), c);

                IEnumerable<MethodInfo> miInfos = ReflectionHelper.GetMethodsWithAttribute(c.GetType(), invokeSystemType);
                foreach (var member in miInfos)
                {
                    InvokeAttribute attr = member.GetCustomAttribute<InvokeAttribute>();
                    string methodName = getUniqueClassString(c) + "." + member.Name;

                    NetworkController.ShowMessage("Register method " + member + " as " + methodName + " with invokeType " + attr.Type);
                    Console.WriteLine("Register method " + member + " as " + methodName + " with invokeType " + attr.Type);

                    methods.Add(methodName, new InvokeHelper(c, member, attr.Type));
                    netCon.RegisterInvoke(methodName, this);
                    Dictionary<string, IMarshalable> res = ReflectionHelper.GetMethodParamsObjects(member);

                    foreach (KeyValuePair<string, IMarshalable> param in res)
                    {
                        if (methodsArguments.ContainsKey(param.Key)) continue;
                        methodsArguments.Add(param.Key, param.Value);
                        NetworkController.ShowMessage($"Register non-basic type {param.Value.GetType()}");
                        Console.WriteLine($"Register non-basic type {param.Value.GetType()}");
                    }
                }

                IEnumerable<FieldInfo> fieldInfos = ReflectionHelper.GetFieldsWithAttribute(c.GetType(), new SyncAttribute().GetType());
                Dictionary<string, InfoHelper> propertyMap = new Dictionary<string, InfoHelper>();
                Console.WriteLine(fieldInfos);
                foreach (var fieldInfo in fieldInfos)
                {
                    Console.WriteLine("Register field " + fieldInfo.Name);
                    NetworkController.ShowMessage("Register field " + fieldInfo.Name);
                    string propName = fieldInfo.Name;
                    propertyMap.Add(propName, new InfoHelper(fieldInfo));
                    var nonBasicObj = ReflectionHelper.CheckNonBasicType(fieldInfo.FieldType);
                    if (nonBasicObj != null)
                    {
                        if (!methodsArguments.ContainsKey(nonBasicObj.ToString()))
                        {
                            methodsArguments.Add(nonBasicObj.ToString(), nonBasicObj as IMarshalable);
                        }
                    }
                }
                IEnumerable<PropertyInfo> propInfos = ReflectionHelper.GetPropertiesWithAttribute(c.GetType(), new SyncAttribute().GetType());
                Console.WriteLine(propInfos);
                foreach (var propInfo in propInfos)
                {
                    Console.WriteLine("Register property " + propInfo.Name);
                    NetworkController.ShowMessage("Register property " + propInfo.Name);
                    string propName = propInfo.Name;
                    propertyMap.Add(propName, new InfoHelper(propInfo));
                    var nonBasicObj = ReflectionHelper.CheckNonBasicType(propInfo.PropertyType);
                    if (nonBasicObj != null)
                    {
                        if (!methodsArguments.ContainsKey(nonBasicObj.ToString()))
                        {
                            methodsArguments.Add(nonBasicObj.ToString(), nonBasicObj as IMarshalable);
                        }
                    }
                }
                properties.Add(getUniqueClassString(c), propertyMap);
                netCon.RegisterInvoke(getSyncMethod(c), this);
            }
            Dispatcher.StartSync(this);
        }

        internal float GetSyncPeriod()
        {
            return syncPeriod;
        }
        public void SetSyncPeriod(float syncPeriod)
        {
            this.syncPeriod = syncPeriod;
            Dispatcher.StopSync(this);
            Dispatcher.StartSync(this);
        }
        ~NetView()
        {
            Dispatcher.StopSync(this);
        }

        private string getUniqueClassString(Object c)
        {
            int hash = c.GetHashCode();
            int num;
            if (!hashToNum.TryGetValue(hash, out num))
            {
                NetworkController.ShowException(new Exception("object not invoked"));
                return null;
            }
            return c.ToString() + "#" + num.ToString();
        }

        internal IMarshalable GetArgument(string name)
        {
            IMarshalable arg;
            if (methodsArguments.TryGetValue(name, out arg))
            {
                return arg;
            }
            return null;
        }

        internal void SyncNow()
        {
            foreach (var one in properties)
            {
                Object c;
                if (!stringToObject.TryGetValue(one.Key, out c))
                {
                    continue;
                }

                string method = getSyncMethod(c);
                DataStorage ds = DataStorage.CreateForWrite();
                ds.Push(method);
                var fields = GetClassFields(c);
                if (fields.Count == 0)
                {
                    continue;
                }
                foreach (var field in fields)
                {
                    ds.Push(field.Key);
                    IMarshalable imObj = field.Value as IMarshalable;
                    if (imObj != null)
                    {
                        pushCustomType(imObj, ds);
                        continue;
                    }
                    pushBasicType(field.Value, ds);
                }
                NetworkController.Instance.SendMessage(ds, (short)MessageType.Resend);
            }
        }
        private string getSyncMethod(Object c)
        {
            return syncMethodPrefix + getUniqueClassString(c);
        }

        internal Dictionary<string, object> GetClassFields(Object c)
        {
            var result = new Dictionary<string, object>();
            Dictionary<string, InfoHelper> props;
            if (properties.TryGetValue(getUniqueClassString(c), out props))
            {
                foreach (KeyValuePair<string, InfoHelper> prop in props)
                {
                    var value = prop.Value.Get(c);
                    string cacheKey = getCacheKeyName(c, prop.Key);

                    //object clone = value;
                    //if (value is ICloneable)
                    //{
                    //    clone = (value as ICloneable).Clone();
                    //}

                    if (!cache.ContainsKey(cacheKey))
                    {
                        cache.Add(cacheKey, value.GetHashCode()) ;
                        result.Add(prop.Key, value);
                    }
                    else if (!cache[cacheKey].Equals(value.GetHashCode()))
                    {
                        cache[cacheKey] = value.GetHashCode();
                        result.Add(prop.Key, value);
                    }
                }
            }

            return result;
        }

        // internal void SetClassFields(string cl, Dictionary<string, object> fields)
        // {
        //     Object c;
        //     if (!stringToObject.TryGetValue(cl, out c))
        //     {
        //         NetworkController.ShowException(new Exception("invalid sync object"));
        //     }
        //     Dictionary<string, InfoHelper> props;
        //     if (!properties.TryGetValue(getUniqueClassString(c), out props))
        //     {
        //         NetworkController.ShowException(new Exception("object's fields hasn't been reflected"));
        //     }
        //     foreach (KeyValuePair<string, object> prop in fields)
        //     {
        //         InfoHelper info;
        //         if (props.TryGetValue(prop.Key, out info))
        //         {
        //             info.Set(c, prop.Value);
        //         }
        //     }
        // }

        private InvokeHelper GetHelper(string method)
        {
            InvokeHelper arg;
            if (methods.TryGetValue(method, out arg))
            {
                return arg;
            }
            return null;
        }

        public void Call(Object c, string method, params object[] args)
        {
            method = getUniqueClassString(c) + "." + method;
            InvokeHelper helper = this.GetHelper(method);
            if (helper == null) return;
            bool client = false;
            bool server = false;
            switch (helper.type)
            {
                case InvokeType.Client:
                    client = true;
                    break;
                case InvokeType.Server:
                    server = true;
                    break;
                case InvokeType.MultiCast:
                    client = true;
                    server = true;
                    break;
            }
            if (client) this.ClientCall(helper, args);
            if (server)
            {
                DataStorage ds = DataStorage.CreateForWrite();
                ds.Push(method);
                foreach (object obj in args)
                {
                    if (IsValidBasicType(obj.GetType()))
                    {
                        pushBasicType(obj, ds);
                        continue;
                    }
                    else
                    {
                        pushCustomType(obj as IMarshalable, ds);
                    }
                }

                NetworkController.Instance.SendMessage(ds, (short)MessageType.Resend);
            }
        }
        private void ClientCall(InvokeHelper helper, params object[] args)
        {
            helper.method.Invoke(helper.classInstance, args);
        }
        internal void RPC(string method, DataStorage request)
        {
            if (method.StartsWith(syncMethodPrefix))
            {
                SyncRPC(method, request);
                //syncRPCThread = new Thread(() => SyncRPC(method, request));
                //syncRPCThread.Start();
                return;
            }
            InvokeHelper helper = this.GetHelper(method);
            if (helper == null) return;
            this.ClientCall(helper, parseRequest(request));
        }

        private void SyncRPC(string method, DataStorage ds)
        {
            var split = method.Split(new string[] { syncMethodPrefix }, 2, StringSplitOptions.None);
            if (split.Length != 2) return;
            string strObj = split[1];
            Object c;
            if (!stringToObject.TryGetValue(strObj, out c)) return;
            Dictionary<string, InfoHelper> props;
            if (!properties.TryGetValue(strObj, out props))
            {
                return;
            }
            while (!ds.Empty)
            {
                string field = ds.ReadString();
                InfoHelper info;
                if (!props.TryGetValue(field, out info))
                {
                    continue;
                }
                string type = info.Type.FullName;
                string dsType = ds.ReadString();
                if (type != dsType) type = dsType;
                object value = parseObject(type, ds);
                if (value == null)
                {
                    continue;
                }
                string cacheKey = getCacheKeyName(c, field);

                //object clone = value;
                //if (value is ICloneable)
                //{
                //    clone = (value as ICloneable).Clone();
                //}

                if (!cache.ContainsKey(cacheKey))
                {
                    cache.Add(cacheKey, value.GetHashCode());
                    info.Set(c, value);
                }
                else if (!cache[cacheKey].Equals(value.GetHashCode()))
                {
                    cache[cacheKey] = value.GetHashCode();
                    info.Set(c, value);
                }
            }
        }

        private object[] parseRequest(DataStorage ds)
        {
            Dictionary<int, object> result = new Dictionary<int, object>();
            int i = 0;
            while (!ds.Empty)
            {

                string key = ds.ReadString();
                object resObject = parseObject(key, ds);
                if (resObject == null)
                {
                    NetworkController.ShowException(new Exception("invalid rpc parameter"));
                }

                result.Add(i, resObject);
                i++;
            }
            object[] resultSlice = new object[i];
            foreach (KeyValuePair<int, object> res in result)
            {
                resultSlice[res.Key] = res.Value;
            }
            return resultSlice;
        }
        private object parseObject(string key, DataStorage ds)
        {
            var obj = GetArgument(key);
            if (obj != null)
            {
                return parseCustomType(obj, ds);
            }
            return parseBasicType(key, ds);
        }
        private object parseCustomType(IMarshalable obj, DataStorage ds)
        {
            obj.ReadFromDs(ds);
            return obj;
        }
        private object parseBasicType(string typ, DataStorage ds)
        {
            return ds.ReadObject(typ);
        }
        private void pushCustomType(IMarshalable obj, DataStorage ds)
        {
            IMarshalable imObj = obj as IMarshalable;
            if (imObj == null)
                NetworkController.ShowException(new Exception("invalid rpc parameter"));

            ds.Push(obj.GetType().FullName, imObj);
        }
        private bool pushBasicType(object obj, DataStorage ds)
        {
            try
            {
                ds.Push(obj);
            }
            catch (Exception e)
            {
                NetworkController.ShowException(e);
                return false;
            }
            return true;
        }
        internal static bool IsValidBasicType(Type type)
        {
            string typ = type.FullName;
            switch (typ)
            {
                case "System.Int32":
                    return true;
                case "System.Byte":
                    return true;
                case "System.Boolean":
                    return true;
                case "System.Char":
                    return true;
                case "System.Decimal":
                    return true;
                case "System.Double":
                    return true;
                case "System.Single":
                    return true;
                case "System.Int64":
                    return true;
                case "System.Int16":
                    return true;
                case "System.String":
                    return true;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return hash;
        }
    }

    internal class InfoHelper
    {
        internal Func<object, object> Get;
        internal Action<object, object> Set;
        internal Type Type;
        internal string Name;

        internal InfoHelper(PropertyInfo prop)
        {
            Get = prop.GetValue;
            Set = prop.SetValue;
            Type = prop.PropertyType;
            Name = prop.Name;
        }
        internal InfoHelper(FieldInfo prop)
        {
            Get = prop.GetValue;
            Set = prop.SetValue;
            Type = prop.FieldType;
            Name = prop.Name;
        }
    }
}