using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace SHA1FileHasherApp
{
    public partial class SHA1Form : Form
    {
        private FileProcessor m_fileProcessor;
        private CancellationTokenSource m_cancellationTokenSource;
        private ResultStore m_resultStore;
        private bool m_isProcessing = false;

        public SHA1Form()
        {
            InitializeComponent();
        }

        private void InitializeFileProcessor()
        {
            m_resultStore = new ResultStore(); 
            m_fileProcessor = new FileProcessor(m_resultStore); 
            m_cancellationTokenSource = new CancellationTokenSource();

            m_fileProcessor.OnDuplicateFound += (hash, file) =>
            {
                this.Invoke(new Action(() =>
                {
                    UpdateTextBox(hash, file); 
                }));
            };
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolder.Text = fbd.SelectedPath; 
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (m_isProcessing)
            {
                Debug.WriteLine("Processing is already running!");
                MessageBox.Show("Processing is already running!");
                return;
            }

            InitializeFileProcessor(); // Újrainicializálás minden indításkor
            txtResult.Clear(); 
            txtResult.AppendText("Processing started...\r\n");

            if (!int.TryParse(txtThreads.Text, out int threadCount) || threadCount <= 0)
            {
                Debug.WriteLine("Invalid thread count specified.");
                MessageBox.Show("Invalid thread count");
                return;
            }

            Debug.WriteLine($"Starting processing with {threadCount} threads.");
            m_isProcessing = true;

            // Producer és consumer szálak indítása
            Task producerTask = Task.Run(() => m_fileProcessor.TraverseDirectory(txtFolder.Text, m_cancellationTokenSource.Token));
            List<Task> consumerTasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                consumerTasks.Add(Task.Run(() => m_fileProcessor.ProcessFiles(m_cancellationTokenSource.Token)));
            }

            try
            {
                await Task.WhenAll(producerTask, Task.WhenAll(consumerTasks)); // Várakozás a szálak befejezésére
                Debug.WriteLine("Processing completed successfully.");
                DisplayResults();
                MessageBox.Show("Processing completed!");
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Any(en => en is OperationCanceledException))
            {
                Debug.WriteLine("Processing canceled by user.");
                DisplayResults(); 
                MessageBox.Show("Processing stopped!");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Processing canceled by user.");
                DisplayResults(); 
                MessageBox.Show("Processing stopped!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error during processing: {ex.Message}");
                MessageBox.Show("An error occurred during processing.");
            }
            finally
            {
                m_isProcessing = false; 
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (m_cancellationTokenSource != null && m_isProcessing)
            {
                m_cancellationTokenSource.Cancel(); 
            }
        }

        private void UpdateTextBox(string p_hash, string p_file)
        {
            txtResult.AppendText($"{p_hash}\r\n");
            txtResult.AppendText($"{p_file}\r\n");
            txtResult.AppendText("\r\n");
        }

        private void DisplayResults()
        {
            txtResult.Clear();
            foreach (var entry in m_resultStore.HashResults.Where(e => e.Value.Count > 1))
            {
                txtResult.AppendText($"{entry.Key}\r\n");
                foreach (string file in entry.Value)
                {
                    txtResult.AppendText($"{file}\r\n");
                }
                txtResult.AppendText("\r\n");
            }
        }
    }
}



