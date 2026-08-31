namespace MissionPlanner.Joystick
{
    partial class Joy_Do_Parachute_Action
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBox_paraAction = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // comboBox_paraAction
            // 
            this.comboBox_paraAction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_paraAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_paraAction.FormattingEnabled = true;
            this.comboBox_paraAction.Location = new System.Drawing.Point(12, 12);
            this.comboBox_paraAction.Name = "comboBox_paraAction";
            this.comboBox_paraAction.Size = new System.Drawing.Size(247, 21);
            this.comboBox_paraAction.TabIndex = 0;
            this.comboBox_paraAction.SelectedIndexChanged += new System.EventHandler(this.comboBox_paraAction_SelectedIndexChanged);
            // 
            // Joy_Do_Parachute_Action
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(271, 48);
            this.Controls.Add(this.comboBox_paraAction);
            this.Name = "Joy_Do_Parachute_Action";
            this.Text = "Joy_Do_Parachute_Action";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox_paraAction;
    }
}