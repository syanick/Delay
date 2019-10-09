namespace Delay
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTarget = new System.Windows.Forms.NumericUpDown();
            this.txtSpeed = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.lblCurrentDelay = new System.Windows.Forms.Label();
            this.btnBuild = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnDump = new System.Windows.Forms.Button();
            this.inputSelector = new System.Windows.Forms.ComboBox();
            this.outputSelector = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.txtDumps = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnCough = new System.Windows.Forms.Button();
            this.lblRampTimer = new System.Windows.Forms.Label();
            this.txtThreshold = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.lblDebug1 = new System.Windows.Forms.Label();
            this.lblDebug2 = new System.Windows.Forms.Label();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.txtTarget)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDumps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(37, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Target Delay";
            // 
            // txtTarget
            // 
            this.txtTarget.DecimalPlaces = 1;
            this.txtTarget.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.txtTarget.Location = new System.Drawing.Point(111, 13);
            this.txtTarget.Maximum = new decimal(new int[] {
            864,
            0,
            0,
            0});
            this.txtTarget.Name = "txtTarget";
            this.txtTarget.Size = new System.Drawing.Size(75, 20);
            this.txtTarget.TabIndex = 1;
            this.txtTarget.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.txtTarget.ValueChanged += new System.EventHandler(this.txtTarget_ValueChanged);
            // 
            // txtSpeed
            // 
            this.txtSpeed.Location = new System.Drawing.Point(111, 40);
            this.txtSpeed.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.txtSpeed.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtSpeed.Name = "txtSpeed";
            this.txtSpeed.Size = new System.Drawing.Size(75, 20);
            this.txtSpeed.TabIndex = 2;
            this.txtSpeed.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.txtSpeed.ValueChanged += new System.EventHandler(this.txtSpeed_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(67, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Speed";
            // 
            // lblCurrentDelay
            // 
            this.lblCurrentDelay.AutoSize = true;
            this.lblCurrentDelay.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentDelay.Location = new System.Drawing.Point(12, 81);
            this.lblCurrentDelay.Name = "lblCurrentDelay";
            this.lblCurrentDelay.Size = new System.Drawing.Size(363, 108);
            this.lblCurrentDelay.TabIndex = 4;
            this.lblCurrentDelay.Text = "000000";
            // 
            // btnBuild
            // 
            this.btnBuild.BackColor = System.Drawing.Color.DarkGreen;
            this.btnBuild.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBuild.ForeColor = System.Drawing.Color.White;
            this.btnBuild.Location = new System.Drawing.Point(30, 236);
            this.btnBuild.Name = "btnBuild";
            this.btnBuild.Size = new System.Drawing.Size(75, 64);
            this.btnBuild.TabIndex = 5;
            this.btnBuild.Text = "Up";
            this.btnBuild.UseVisualStyleBackColor = false;
            this.btnBuild.Click += new System.EventHandler(this.btnBuild_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Olive;
            this.btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.Location = new System.Drawing.Point(111, 236);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 64);
            this.btnExit.TabIndex = 6;
            this.btnExit.Text = "Down";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnDump
            // 
            this.btnDump.BackColor = System.Drawing.Color.Maroon;
            this.btnDump.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDump.ForeColor = System.Drawing.Color.White;
            this.btnDump.Location = new System.Drawing.Point(273, 236);
            this.btnDump.Name = "btnDump";
            this.btnDump.Size = new System.Drawing.Size(75, 64);
            this.btnDump.TabIndex = 8;
            this.btnDump.Text = "DUMP";
            this.btnDump.UseVisualStyleBackColor = false;
            this.btnDump.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnDump_Click);
            this.btnDump.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnCough_MouseUp);
            // 
            // inputSelector
            // 
            this.inputSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.inputSelector.FormattingEnabled = true;
            this.inputSelector.Location = new System.Drawing.Point(86, 332);
            this.inputSelector.Name = "inputSelector";
            this.inputSelector.Size = new System.Drawing.Size(262, 21);
            this.inputSelector.TabIndex = 9;
            this.inputSelector.SelectedIndexChanged += new System.EventHandler(this.inputSelector_SelectedIndexChanged);
            // 
            // outputSelector
            // 
            this.outputSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.outputSelector.FormattingEnabled = true;
            this.outputSelector.Location = new System.Drawing.Point(86, 360);
            this.outputSelector.Name = "outputSelector";
            this.outputSelector.Size = new System.Drawing.Size(262, 21);
            this.outputSelector.TabIndex = 10;
            this.outputSelector.SelectedIndexChanged += new System.EventHandler(this.outputSelector_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(49, 335);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(31, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Input";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(41, 363);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(39, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Output";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // txtDumps
            // 
            this.txtDumps.Cursor = System.Windows.Forms.Cursors.Default;
            this.txtDumps.Location = new System.Drawing.Point(308, 306);
            this.txtDumps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtDumps.Name = "txtDumps";
            this.txtDumps.Size = new System.Drawing.Size(40, 20);
            this.txtDumps.TabIndex = 13;
            this.txtDumps.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtDumps.ValueChanged += new System.EventHandler(this.numericUpDown3_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(262, 308);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Dumps";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(30, 193);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(318, 23);
            this.progressBar1.TabIndex = 15;
            // 
            // btnCough
            // 
            this.btnCough.BackColor = System.Drawing.Color.DarkBlue;
            this.btnCough.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCough.ForeColor = System.Drawing.Color.White;
            this.btnCough.Location = new System.Drawing.Point(192, 236);
            this.btnCough.Name = "btnCough";
            this.btnCough.Size = new System.Drawing.Size(75, 64);
            this.btnCough.TabIndex = 16;
            this.btnCough.Text = "Cough";
            this.btnCough.UseVisualStyleBackColor = false;
            this.btnCough.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnCough_MouseDown);
            this.btnCough.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnCough_MouseUp);
            // 
            // lblRampTimer
            // 
            this.lblRampTimer.BackColor = System.Drawing.Color.Transparent;
            this.lblRampTimer.Location = new System.Drawing.Point(27, 219);
            this.lblRampTimer.Name = "lblRampTimer";
            this.lblRampTimer.Size = new System.Drawing.Size(321, 14);
            this.lblRampTimer.TabIndex = 17;
            this.lblRampTimer.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // txtThreshold
            // 
            this.txtThreshold.DecimalPlaces = 1;
            this.txtThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.txtThreshold.Location = new System.Drawing.Point(111, 66);
            this.txtThreshold.Maximum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.txtThreshold.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.txtThreshold.Name = "txtThreshold";
            this.txtThreshold.Size = new System.Drawing.Size(75, 20);
            this.txtThreshold.TabIndex = 19;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(27, 68);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Silence Thresh";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(286, 66);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(62, 20);
            this.numericUpDown1.TabIndex = 20;
            this.numericUpDown1.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(192, 68);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 13);
            this.label7.TabIndex = 21;
            this.label7.Text = "Ramp Smoothing";
            // 
            // lblDebug1
            // 
            this.lblDebug1.AutoSize = true;
            this.lblDebug1.Location = new System.Drawing.Point(195, 15);
            this.lblDebug1.Name = "lblDebug1";
            this.lblDebug1.Size = new System.Drawing.Size(13, 13);
            this.lblDebug1.TabIndex = 22;
            this.lblDebug1.Text = "0";
            // 
            // lblDebug2
            // 
            this.lblDebug2.AutoSize = true;
            this.lblDebug2.Location = new System.Drawing.Point(195, 41);
            this.lblDebug2.Name = "lblDebug2";
            this.lblDebug2.Size = new System.Drawing.Size(21, 13);
            this.lblDebug2.TabIndex = 23;
            this.lblDebug2.Text = "0%";
            // 
            // timer2
            // 
            this.timer2.Enabled = true;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(383, 388);
            this.Controls.Add(this.lblDebug2);
            this.Controls.Add(this.lblDebug1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.txtThreshold);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblRampTimer);
            this.Controls.Add(this.btnCough);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtDumps);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.outputSelector);
            this.Controls.Add(this.inputSelector);
            this.Controls.Add(this.btnDump);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnBuild);
            this.Controls.Add(this.lblCurrentDelay);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtSpeed);
            this.Controls.Add(this.txtTarget);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.RightToLeftLayout = true;
            this.Text = "@#$% Delay";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.txtTarget)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpeed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDumps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown txtTarget;
        private System.Windows.Forms.NumericUpDown txtSpeed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblCurrentDelay;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnDump;
        private System.Windows.Forms.ComboBox inputSelector;
        private System.Windows.Forms.ComboBox outputSelector;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.NumericUpDown txtDumps;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnCough;
        private System.Windows.Forms.Label lblRampTimer;
        private System.Windows.Forms.NumericUpDown txtThreshold;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblDebug1;
        private System.Windows.Forms.Label lblDebug2;
        private System.Windows.Forms.Timer timer2;
    }
}

