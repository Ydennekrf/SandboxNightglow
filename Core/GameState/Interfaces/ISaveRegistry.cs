

using System.Collections.Generic;

namespace ethra.V1
{
    public interface ISaveRegistry
    {
        IReadOnlyList<ISaveable> All { get; }
        void Register(ISaveable s);
        void Unregister(ISaveable s);
    }
}