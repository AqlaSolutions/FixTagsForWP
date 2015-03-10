using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace FixTagsForWP
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void TestForm_Load(object sender, EventArgs e)
        {
            Visible = false;
            Size = new Size(0, 0);
            WMP.settings.volume = 0;
        }

        public void RestartTest(string filename)
        {
            Reset();

            WMP.PlayStateChange += WMP_PlayStateChange;
            WMP.MediaError += WMP_MediaError;

            WMP.URL = filename;
            WMP.Ctlcontrols.play();
        }

        public struct TestCompletedEventArgs
        {
            public bool Success { get; private set; }
            public string Filename { get; private set; }

            public TestCompletedEventArgs(bool success, string filename)
                : this()
            {
                Success = success;
                Filename = filename;
            }
        }


        public event EventHandler<TestCompletedEventArgs> Completed;

        void WMP_MediaError(object sender, AxWMPLib._WMPOCXEvents_MediaErrorEvent e)
        {
            Reset();
            var d = Completed;
            if (d != null)
                d(this, new TestCompletedEventArgs(false, WMP.URL));
        }

        void WMP_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            switch (WMP.playState)
            {
                case WMPPlayState.wmppsPlaying:
                    {
                        Reset();
                        var d = Completed;
                        if (d != null)
                            d(this, new TestCompletedEventArgs(true, WMP.URL));
                    }
                    break;
            }
        }

        private void Reset()
        {
            WMP.PlayStateChange -= WMP_PlayStateChange;
            WMP.MediaError -= WMP_MediaError;
            WMP.Ctlcontrols.stop();
        }
    }
}
