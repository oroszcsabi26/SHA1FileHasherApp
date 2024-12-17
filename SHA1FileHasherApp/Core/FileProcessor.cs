using SHA1FileHasherApp.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SHA1FileHasherApp
{
    public class FileProcessor
    {
        private BlockingCollection<string> m_fileQueue;
        private ResultStore m_resultStore;
        private SHA1Hasher m_hasher;
        public int m_bytesToRead = 128;
        public event Action<string, string> OnDuplicateFound;

        public FileProcessor(ResultStore p_store)
        {
            m_fileQueue = new BlockingCollection<string>();
            m_resultStore = p_store;
            m_hasher = new SHA1Hasher();
        }

        public void TraverseDirectory(string p_path, CancellationToken p_token)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(p_path, "*.*", SearchOption.AllDirectories))
                {
                    if (p_token.IsCancellationRequested)
                    {
                        Debug.WriteLine($"Cancellation requested. Stopping directory traversal.");
                        throw new OperationCanceledException(p_token); 
                    }
                    
                    // Fájl hozzáadása a gyűjteményhez
                    if (!m_fileQueue.TryAdd(file))
                    {
                        Debug.WriteLine($"Failed to add file: {file}");
                    }
                    else
                    {
                        Debug.WriteLine($"File added to queue: {file}");
                    }
                }
            }
            finally
            {
                Debug.WriteLine("Directory traversal completed. Marking collection as complete.");
                m_fileQueue.CompleteAdding(); // jelezzük a fogyasztóknak, hogy nincs több fájl
            }
        }


        public void ProcessFiles(CancellationToken p_token)
        {
            try
            {
                foreach (string file in m_fileQueue.GetConsumingEnumerable(p_token)) // Automatikusan ellenőrzi a token állapotát
                {
                    if (p_token.IsCancellationRequested)
                    {
                        Debug.WriteLine("Cancellation requested. Stopping file processing.");
                        throw new OperationCanceledException(p_token); 
                    }

                    string hash = m_hasher.ComputeSHA1(file, m_bytesToRead);
                    if (!string.IsNullOrEmpty(hash))
                    {
                        bool hasDuplicate = m_resultStore.AddResult(hash, file);
                        if (hasDuplicate)
                        {
                            Debug.WriteLine($"Duplicate found: {file} (Hash: {hash})");
                            OnDuplicateFound?.Invoke(hash, file);
                        }
                        else
                        {
                            Debug.WriteLine($"File processed successfully: {file} (Hash: {hash})");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to compute hash for file: {file}");
                    }

                    Task.Delay(50, p_token).Wait(); 
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("File processing canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during file processing: {ex.Message}");
                throw;
            }
        }
    }
}


