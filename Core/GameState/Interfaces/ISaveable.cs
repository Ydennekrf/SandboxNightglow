

namespace ethra.V1
{
    public interface ISaveable
    {
        string SaveKey { get; }
        object CaptureSnapshot();
        void RestoreSnapshot(object snapshot);
    }
}