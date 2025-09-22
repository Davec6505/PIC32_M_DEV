using System;
using System.Drawing;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{
    public class CommitDialog : Form
    {
        private TextBox txtMessage;
        private TextBox txtName;
        private TextBox txtEmail;
        private Button btnOk;
        private Button btnCancel;

        public string Message => txtMessage.Text.Trim();
        public string AuthorName => txtName.Text.Trim();
        public string AuthorEmail => txtEmail.Text.Trim();

        public CommitDialog()
        {
            Text = "Commit";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 360);

            var lblMsg = new Label { Text = "Message:", AutoSize = true, Location = new Point(12, 12) };
            txtMessage = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(12, 34), Size = new Size(496, 220) };

            var lblName = new Label { Text = "Author Name:", AutoSize = true, Location = new Point(12, 264) };
            txtName = new TextBox { Location = new Point(120, 260), Size = new Size(200, 27) };

            var lblEmail = new Label { Text = "Author Email:", AutoSize = true, Location = new Point(12, 298) };
            txtEmail = new TextBox { Location = new Point(120, 294), Size = new Size(200, 27) };

            btnOk = new Button { Text = "Commit", DialogResult = DialogResult.OK, Location = new Point(348, 290), Size = new Size(75, 30) };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(433, 290), Size = new Size(75, 30) };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(lblMsg);
            Controls.Add(txtMessage);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblEmail);
            Controls.Add(txtEmail);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }
    }
}
