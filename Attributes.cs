using System;
using System.Reflection;

namespace GServer
{
    public enum InvokeType
    {
        Client,
        Server,
        MultiCast
    }
    public class InvokeAttribute: Attribute
    {
        public InvokeType Type { get; set; }

        public InvokeAttribute()
        {
            Type = InvokeType.MultiCast;
        }

        public InvokeAttribute(InvokeType invokeType)
        {
            Type = invokeType;
        }
    }

    public class InvokeHelper
    {
        public Object classInstance;
        public MethodInfo method;
        public InvokeType type;
        public InvokeHelper(Object instance, MethodInfo methodInfo, InvokeType invokeType)
        {
            classInstance = instance;
            method = methodInfo;
            type = invokeType;
        }
    }

    
    public enum SyncType
    {
        Receive,
        Send,
        Exchange
    }

    public class SyncAttribute : Attribute {}

    public class SyncHelper
    {
        public Object Class;
        public PropertyInfo Field;

        public SyncHelper() {}

        public SyncHelper(Object c, PropertyInfo field)
        {
            Class = c;
            Field = field;
        }
    }
}