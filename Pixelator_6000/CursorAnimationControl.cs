using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pixelator_6000
{
    //Implement this?
    public class CursorAnimationControl : IDisposable
    {
        private Cursor _previousCursor;

        public void WaitCursor()
        {
            _previousCursor = Mouse.OverrideCursor;

            Mouse.OverrideCursor = Cursors.Wait;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Mouse.OverrideCursor = _previousCursor;
        }

        #endregion
        
    }
}
