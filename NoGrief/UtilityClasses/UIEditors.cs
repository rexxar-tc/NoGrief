using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace NoGriefPlugin.UtilityClasses
{
    internal class TimePickerEditor : UITypeEditor
    {
        private IWindowsFormsEditorService editorService;
        private string time;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            }
            if (editorService != null)
            {
                if (value == null)
                {
                    time = DateTime.Now.ToString("HH:mm");
                }

                var picker = new DateTimePicker();
                picker.Format = DateTimePickerFormat.Custom;
                picker.CustomFormat = "HH:mm";
                picker.ShowUpDown = true;

                if (value != null)
                {
                    picker.Value = DateTime.Parse((string)value);
                }

                editorService.DropDownControl(picker);
                value = picker.Value.ToString("HH:mm");
            }
            return value;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }
}