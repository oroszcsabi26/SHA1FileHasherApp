using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SHA1FileHasherApp.Core.Interfaces
{
    public interface IResultStore
    {
        bool AddResult(string p_hash, string p_file);

        ConcurrentDictionary<string, List<string>> HashResults { get; }
    }
}
