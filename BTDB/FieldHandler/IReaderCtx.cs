using BTDB.StreamLayer;

namespace BTDB.FieldHandler
{
    public interface IReaderCtx
    {
        // Returns true if actual content needs to be deserialized
        bool ReadObject(out object @object);
        // Register last deserialized object
        void RegisterObject(object @object);
        void ReadObjectDone();
        // Returns true if actual content needs to be deserialized
        bool SkipObject();
        
        AbstractBufferedReader Reader();
    }
}