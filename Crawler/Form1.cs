using System;
using System.Windows.Forms;
#region tasks
/*
‐ DOM typu <IMG>
‐ DOM typu <A> do stron, które kończą się na .html lub .htm
‐ DOM typu <A> będące odnośnikami mailowymi
‐ adresy email, które nie są zamieszczone w elemencie <A>
 -Program powinien rekurencyjnie utworzyć drzewo strony (wraz z podstronami) i zapisać je do wynikowego pliku XML wg schematu:
‐ korzeń pliku XML ‐ węzeł SITE z atrybutem url zawierającym adres strony oraz depth zawierającym głębokość
‐ węzły <IMAGE> zawierające nazwę i rozszerzenia obrazków (png, bmp, jpg, jpeg, gif)
‐ węzły <EMAIL> zawierające adresy email zebrane przez crawlera
‐ węzły <FILE> dla elementów <A> i prowadzące do stron .html lub .htm. Węzły <FILE> powinny być rekurencyjnie uzupełniane wg tego samego schematu.
*/
#endregion
namespace Crawler
{
    public partial class Form1 : Form
    {
        Client client = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            client = new Client(textBox1.Text,int.Parse(textBox2.Text));
            textBox3.Text = client.CreateXML(int.Parse(textBox2.Text));
        }
    }
}
