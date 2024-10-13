namespace FF
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            OptimizedButton = new Button();
            DefaultButton = new Button();
            AcceptDelayButton = new Button();
            PickingTypeLabel = new Label();
            DelayTextBox = new TextBox();
            StatusLabel = new Label();
            WarehousePictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)WarehousePictureBox).BeginInit();
            SuspendLayout();
            // 
            // OptimizedButton
            // 
            OptimizedButton.Cursor = Cursors.Hand;
            OptimizedButton.Font = new Font("Century Gothic", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 204);
            OptimizedButton.ForeColor = SystemColors.HotTrack;
            OptimizedButton.Location = new Point(21, 62);
            OptimizedButton.Margin = new Padding(4);
            OptimizedButton.Name = "OptimizedButton";
            OptimizedButton.Size = new Size(180, 50);
            OptimizedButton.TabIndex = 0;
            OptimizedButton.Text = "Optimized";
            OptimizedButton.UseVisualStyleBackColor = true;
            OptimizedButton.Click += OptimizedButton_Click;
            // 
            // DefaultButton
            // 
            DefaultButton.Cursor = Cursors.Hand;
            DefaultButton.Font = new Font("Century Gothic", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 204);
            DefaultButton.ForeColor = SystemColors.HotTrack;
            DefaultButton.Location = new Point(209, 62);
            DefaultButton.Margin = new Padding(4);
            DefaultButton.Name = "DefaultButton";
            DefaultButton.Size = new Size(180, 50);
            DefaultButton.TabIndex = 1;
            DefaultButton.Text = "Default";
            DefaultButton.UseVisualStyleBackColor = true;
            DefaultButton.Click += DefaultButton_Click;
            // 
            // AcceptDelayButton
            // 
            AcceptDelayButton.Cursor = Cursors.Hand;
            AcceptDelayButton.Font = new Font("Century Gothic", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 204);
            AcceptDelayButton.ForeColor = SystemColors.HotTrack;
            AcceptDelayButton.Location = new Point(516, 62);
            AcceptDelayButton.Margin = new Padding(4);
            AcceptDelayButton.Name = "AcceptDelayButton";
            AcceptDelayButton.Size = new Size(280, 50);
            AcceptDelayButton.TabIndex = 2;
            AcceptDelayButton.Text = "Accept";
            AcceptDelayButton.UseVisualStyleBackColor = true;
            AcceptDelayButton.Click += AcceptDelayButton_Click;
            // 
            // PickingTypeLabel
            // 
            PickingTypeLabel.AutoSize = true;
            PickingTypeLabel.Location = new Point(21, 24);
            PickingTypeLabel.Name = "PickingTypeLabel";
            PickingTypeLabel.Size = new Size(262, 23);
            PickingTypeLabel.TabIndex = 3;
            PickingTypeLabel.Text = "Choose picking type mode";
            // 
            // DelayTextBox
            // 
            DelayTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            DelayTextBox.BorderStyle = BorderStyle.None;
            DelayTextBox.CharacterCasing = CharacterCasing.Lower;
            DelayTextBox.Cursor = Cursors.IBeam;
            DelayTextBox.ForeColor = Color.YellowGreen;
            DelayTextBox.Location = new Point(516, 24);
            DelayTextBox.MaxLength = 4;
            DelayTextBox.Name = "DelayTextBox";
            DelayTextBox.PlaceholderText = "Drawing delay | ms";
            DelayTextBox.Size = new Size(260, 24);
            DelayTextBox.TabIndex = 4;
            DelayTextBox.TextAlign = HorizontalAlignment.Center;
            DelayTextBox.TextChanged += DelayTextBox_TextChanged;
            // 
            // StatusLabel
            // 
            StatusLabel.AutoSize = true;
            StatusLabel.BackColor = Color.Crimson;
            StatusLabel.Location = new Point(396, 62);
            StatusLabel.Name = "StatusLabel";
            StatusLabel.Size = new Size(66, 23);
            StatusLabel.TabIndex = 6;
            StatusLabel.Text = "Wait...";
            // 
            // WarehousePictureBox
            // 
            WarehousePictureBox.BackColor = Color.Navy;
            WarehousePictureBox.Cursor = Cursors.No;
            WarehousePictureBox.Location = new Point(23, 119);
            WarehousePictureBox.Name = "WarehousePictureBox";
            WarehousePictureBox.Size = new Size(2330, 1190);
            WarehousePictureBox.TabIndex = 7;
            WarehousePictureBox.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.MidnightBlue;
            ClientSize = new Size(2544, 1401);
            Controls.Add(WarehousePictureBox);
            Controls.Add(StatusLabel);
            Controls.Add(DelayTextBox);
            Controls.Add(PickingTypeLabel);
            Controls.Add(AcceptDelayButton);
            Controls.Add(DefaultButton);
            Controls.Add(OptimizedButton);
            Font = new Font("Century Gothic", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 204);
            ForeColor = SystemColors.ButtonHighlight;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Fulfillment simulator";
            ((System.ComponentModel.ISupportInitialize)WarehousePictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button OptimizedButton;
        private Button DefaultButton;
        private Button AcceptDelayButton;
        private Label PickingTypeLabel;
        private TextBox DelayTextBox;
        private Label StatusLabel;
        private PictureBox WarehousePictureBox;
    }
}
