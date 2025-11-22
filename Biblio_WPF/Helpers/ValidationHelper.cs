using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Biblio_WPF.Helpers
{
    public static class ValidationHelper
    {
        public static void ResetValidationVisuals(params Control[] controls)
        {
            if (controls == null) return;
            foreach (var ctl in controls)
            {
                if (ctl == null) continue;
                ctl.ClearValue(Border.BorderBrushProperty);
                ctl.ClearValue(Border.BorderThicknessProperty);
            }
        }

        public static void MarkInvalid(Control ctl)
        {
            if (ctl == null) return;
            if (ctl is TextBox tb)
            {
                tb.BorderBrush = Brushes.Red;
                tb.BorderThickness = new Thickness(1);
                tb.Focus();
            }
            else if (ctl is PasswordBox pb)
            {
                pb.BorderBrush = Brushes.Red;
                pb.BorderThickness = new Thickness(1);
                pb.Focus();
            }
            else if (ctl is ComboBox cb)
            {
                cb.BorderBrush = Brushes.Red;
                cb.BorderThickness = new Thickness(1);
                cb.Focus();
            }
            else if (ctl is DatePicker dp)
            {
                dp.BorderBrush = Brushes.Red;
                dp.BorderThickness = new Thickness(1);
                dp.Focus();
            }
        }
    }
}
