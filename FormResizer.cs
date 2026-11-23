using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayQuest
{
    public class FormResizer
    {
        private readonly Form _form;
        private readonly Size _originalClientSize;
        private readonly Dictionary<Control, (Rectangle Bounds, float FontSize)> _originalStates = new();

        public FormResizer(Form form)
        {
            _form = form;
            _originalClientSize = form.ClientSize;

            CaptureControls(_form);
            _form.Resize += OnFormResize;
        }

        private void CaptureControls(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                _originalStates[ctrl] = (ctrl.Bounds, ctrl.Font.Size);
                if (ctrl.HasChildren)
                {
                    CaptureControls(ctrl);
                }
            }
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            if (_originalClientSize.Width <= 0 || _originalClientSize.Height <= 0) return;
            if (_form.ClientSize.Width <= 0 || _form.ClientSize.Height <= 0) return;

            float scaleX = (float)_form.ClientSize.Width / _originalClientSize.Width;
            float scaleY = (float)_form.ClientSize.Height / _originalClientSize.Height;
            float fontScale = Math.Min(scaleX, scaleY);

            _form.SuspendLayout();
            foreach (var entry in _originalStates)
            {
                Control ctrl = entry.Key;
                Rectangle orig = entry.Value.Bounds;
                float origFont = entry.Value.FontSize;

                ctrl.Bounds = new Rectangle(
                    (int)Math.Round(orig.X * scaleX),
                    (int)Math.Round(orig.Y * scaleY),
                    (int)Math.Round(orig.Width * scaleX),
                    (int)Math.Round(orig.Height * scaleY)
                );

                float newFontSize = Math.Max(7f, origFont * fontScale);
                if (Math.Abs(ctrl.Font.Size - newFontSize) > 0.25f)
                {
                    ctrl.Font = new Font(ctrl.Font.FontFamily, newFontSize, ctrl.Font.Style);
                }
            }
            _form.ResumeLayout();
        }
    }
}