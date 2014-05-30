namespace Practicum1
{
    partial class Main
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
            this.label1 = new System.Windows.Forms.Label();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.goButton = new System.Windows.Forms.Button();
            this.resultViewDataGrid = new System.Windows.Forms.DataGridView();
            this.Score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mpg = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cylinders = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.displacement = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.horsepower = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.weight = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.acceleration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.model_year = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.origin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.brand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.model = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.resultViewDataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(44, 37);
            this.label1.MinimumSize = new System.Drawing.Size(100, 100);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 100);
            this.label1.TabIndex = 0;
            this.label1.Tag = "test";
            // 
            // inputTextBox
            // 
            this.inputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inputTextBox.Location = new System.Drawing.Point(12, 650);
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.Size = new System.Drawing.Size(1066, 20);
            this.inputTextBox.TabIndex = 1;
            // 
            // goButton
            // 
            this.goButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.goButton.Location = new System.Drawing.Point(1084, 650);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(168, 20);
            this.goButton.TabIndex = 2;
            this.goButton.Text = "GO!";
            this.goButton.UseVisualStyleBackColor = true;
            this.goButton.Click += new System.EventHandler(this.goButton_Click);
            // 
            // resultViewDataGrid
            // 
            this.resultViewDataGrid.AllowUserToAddRows = false;
            this.resultViewDataGrid.AllowUserToDeleteRows = false;
            this.resultViewDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resultViewDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.resultViewDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Score,
            this.mpg,
            this.cylinders,
            this.displacement,
            this.horsepower,
            this.weight,
            this.acceleration,
            this.model_year,
            this.origin,
            this.brand,
            this.model,
            this.type});
            this.resultViewDataGrid.Location = new System.Drawing.Point(12, 12);
            this.resultViewDataGrid.Name = "resultViewDataGrid";
            this.resultViewDataGrid.ReadOnly = true;
            this.resultViewDataGrid.RowHeadersWidth = 75;
            this.resultViewDataGrid.Size = new System.Drawing.Size(1240, 632);
            this.resultViewDataGrid.TabIndex = 3;
            // 
            // Score
            // 
            this.Score.HeaderText = "Score";
            this.Score.Name = "Score";
            this.Score.ReadOnly = true;
            // 
            // mpg
            // 
            this.mpg.HeaderText = "mpg";
            this.mpg.Name = "mpg";
            this.mpg.ReadOnly = true;
            // 
            // cylinders
            // 
            this.cylinders.HeaderText = "cylinders";
            this.cylinders.Name = "cylinders";
            this.cylinders.ReadOnly = true;
            // 
            // displacement
            // 
            this.displacement.HeaderText = "displacement";
            this.displacement.Name = "displacement";
            this.displacement.ReadOnly = true;
            // 
            // horsepower
            // 
            this.horsepower.HeaderText = "horsepower";
            this.horsepower.Name = "horsepower";
            this.horsepower.ReadOnly = true;
            // 
            // weight
            // 
            this.weight.HeaderText = "weight";
            this.weight.Name = "weight";
            this.weight.ReadOnly = true;
            // 
            // acceleration
            // 
            this.acceleration.HeaderText = "acceleration";
            this.acceleration.Name = "acceleration";
            this.acceleration.ReadOnly = true;
            // 
            // model_year
            // 
            this.model_year.HeaderText = "model_year";
            this.model_year.Name = "model_year";
            this.model_year.ReadOnly = true;
            // 
            // origin
            // 
            this.origin.HeaderText = "origin";
            this.origin.Name = "origin";
            this.origin.ReadOnly = true;
            // 
            // brand
            // 
            this.brand.HeaderText = "brand";
            this.brand.Name = "brand";
            this.brand.ReadOnly = true;
            // 
            // model
            // 
            this.model.HeaderText = "model";
            this.model.Name = "model";
            this.model.ReadOnly = true;
            // 
            // type
            // 
            this.type.HeaderText = "type";
            this.type.Name = "type";
            this.type.ReadOnly = true;
            // 
            // Main
            // 
            this.AcceptButton = this.goButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 682);
            this.Controls.Add(this.resultViewDataGrid);
            this.Controls.Add(this.goButton);
            this.Controls.Add(this.inputTextBox);
            this.Controls.Add(this.label1);
            this.MinimumSize = new System.Drawing.Size(100, 100);
            this.Name = "Main";
            this.Tag = "test";
            ((System.ComponentModel.ISupportInitialize)(this.resultViewDataGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox inputTextBox;
        private System.Windows.Forms.Button goButton;
        private System.Windows.Forms.DataGridView resultViewDataGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn Score;
        private System.Windows.Forms.DataGridViewTextBoxColumn mpg;
        private System.Windows.Forms.DataGridViewTextBoxColumn cylinders;
        private System.Windows.Forms.DataGridViewTextBoxColumn displacement;
        private System.Windows.Forms.DataGridViewTextBoxColumn horsepower;
        private System.Windows.Forms.DataGridViewTextBoxColumn weight;
        private System.Windows.Forms.DataGridViewTextBoxColumn acceleration;
        private System.Windows.Forms.DataGridViewTextBoxColumn model_year;
        private System.Windows.Forms.DataGridViewTextBoxColumn origin;
        private System.Windows.Forms.DataGridViewTextBoxColumn brand;
        private System.Windows.Forms.DataGridViewTextBoxColumn model;
        private System.Windows.Forms.DataGridViewTextBoxColumn type;
    }
}

