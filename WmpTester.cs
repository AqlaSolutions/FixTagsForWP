using System;
using System.Threading;
using System.Windows.Forms;

namespace FixTagsForWP
{
    public class WmpTester : IDisposable
    {
        readonly SynchronizationContext _uiContext;
        private TestForm _form;

        public WmpTester(SynchronizationContext uiContext)
        {
            if (uiContext == null) throw new ArgumentNullException("uiContext");
            _uiContext = uiContext;

            uiContext.Send(
                state =>
                {
                    _form = new TestForm();
                    _form.Completed += (s, a) => _completed(s, a);
                }, null);
        }

        volatile EventHandler<TestForm.TestCompletedEventArgs> _completed = delegate { };

        bool _entered;

        public bool CanBeRead(string filename)
        {
            if (_disposed) throw new ObjectDisposedException(ToString());
            if (_entered) // no sta reentrance
                throw new InvalidOperationException("No parallel calls allowed");
            _entered = true;
            try
            {
                bool r = false;
                using (var ev = new ManualResetEventSlim())
                {
                    _uiContext.Send(
                        state =>
                        {
                            _completed = (s, a) =>
                                {
                                    Thread.MemoryBarrier();
                                    r = a.Success;
                                    ev.Set();
                                };

                            _form.RestartTest(filename);
                        }, null);
                    if (SynchronizationContext.Current == _uiContext)
                    {
                        while (!ev.IsSet)
                        {
                            Application.DoEvents();
                        }
                    }
                    else ev.Wait();
                }
                return r;
            }
            finally
            {
                _entered = false;
            }
        }

        bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _uiContext.Send(s => _form.Dispose(), null);
        }
    }
}