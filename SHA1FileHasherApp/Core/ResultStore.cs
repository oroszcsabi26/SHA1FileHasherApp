using SHA1FileHasherApp.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SHA1FileHasherApp
{
    public class ResultStore : IResultStore
    {
        private ConcurrentDictionary<string, List<string>> m_hashResults = new ConcurrentDictionary<string, List<string>>();

        public ConcurrentDictionary<string, List<string>> HashResults => m_hashResults;

        public bool AddResult(string p_hash, string p_file)
        {
            var list = m_hashResults.GetOrAdd(p_hash, _ => new List<string>());
            lock (list)
            {
                list.Add(p_file);
                return list.Count > 1;
            }
        }
    }
}
