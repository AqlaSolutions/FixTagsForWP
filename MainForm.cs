using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Id3;

namespace FixTagsForWP
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length != 1) return;
            var path = args[0];
            Process(path);
            Application.Exit();
        }

        public class CantBeFixedException : Exception
        {
            public CantBeFixedException()
            {
            }

            protected CantBeFixedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        void Process(string path)
        {
            OutputBox.Items.Clear();
            SynchronizationContext context = SynchronizationContext.Current;
            var backups = new List<string>();
            using (var tester = new WmpTester(context))
            {
                foreach (var f in Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories))
                {
                    if (!tester.CanBeRead(f))
                    {
                        OutputBox.Items.Add("Fixing: " + f);
                        Application.DoEvents();

                        var backupF = f + ".bck";
                        File.Copy(f, backupF, true);
                        try
                        {
                            using (var mp3 = new Mp3File(f, Mp3Permissions.ReadWrite))
                            {
                                mp3.DeleteTag(Id3TagFamily.Version2x);
                            }
                            if (!tester.CanBeRead(f))
                                throw new CantBeFixedException();
                            backups.Add(backupF);
                        }
                        catch (Exception e)
                        {
                            try { File.Delete(f); }
                            catch { }
                            File.Move(backupF, f);
                            OutputBox.Items.Add(e.ToString());
                        }
                    }
                }
            }

            OutputBox.Items.Add("Finished.");

            if (MessageBox.Show("Remove .bck backups?", "Finished", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (var f in backups)
                {
                    try { File.Delete(f); }
                    catch { }
                }
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (FolderBrowser.ShowDialog() != DialogResult.OK) return;
            StartButton.Enabled = false;
            try
            {
                Process(FolderBrowser.SelectedPath);
            }
            finally
            {
                StartButton.Enabled = true;
            }
        }
    }
}
