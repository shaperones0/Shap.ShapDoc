using System.Xml;

namespace Shap.ShapDoc.CodeConverter
{
    public partial class MainWindow : Form
    {
        readonly CodeConverter converter = new();

        public MainWindow()
        {
            InitializeComponent();

            cbLang.SelectedIndex = 0;
        }

        private void btnPaste_Click(object sender, EventArgs e)
        {
            string toConvert = rtbRezult.Text != "" ? 
                toConvert = rtbRezult.Text : 
                toConvert = Clipboard.GetText(TextDataFormat.Html);

            rtbRezult.Text = converter.Convert(toConvert, cbLang.SelectedItem!.ToString()!);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(rtbRezult.Text);
        }
    }
}
