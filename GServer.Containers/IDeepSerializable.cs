namespace GServer.Containers
{
    public interface IDeepSerializable
    {
        void PushToDs(DataStorage ds);
    }

    public interface IDeepDeserializable
    {
        void ReadFromDs(DataStorage ds);
    }

    public interface IMarshalable : IDeepSerializable, IDeepDeserializable
    {
    }
}