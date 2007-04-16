using System;
using System.Text.RegularExpressions;

namespace Flatarity
{
  public partial class LevelForm : System.Windows.Forms.Form
  {
    public LevelForm()
    {
      InitializeComponent();
    }

    public int Level
    {
      get { return txtLevel.TextLength == 0 ? 1 : int.Parse(txtLevel.Text); }
      set { txtLevel.Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    }

    private void txtLevel_TextChanged(object sender, EventArgs e)
    {
      string filtered = nonDigit.Replace(txtLevel.Text, "");
      if(filtered.Length != txtLevel.TextLength) txtLevel.Text = filtered;
    }

    readonly Regex nonDigit = new Regex(@"\D");
  }
}