using System;

namespace PommaLabs.KVLite.Examples.WebForms
{
    public partial class Default : PageWithCustomViewStatePersister
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Refresh();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            lblDateTime.Text = $"UTC Now: {DateTime.UtcNow}";
        }
    }
}