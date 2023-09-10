﻿namespace uni.ui.common {
  public partial class ColorPicker : UserControl {
    public ColorPicker() {
      InitializeComponent();
     
      this.Value = Color.White;

      this.Click += (_, _) => this.ShowColorPickerDialog_();
    }

    public Color Value {
      get => this.BackColor;
      set => this.BackColor = value;
    }

    private void ShowColorPickerDialog_() {
      var colorDialog = new ColorDialog {
          Color = this.Value,
          SolidColorOnly = true,
      };

      var result = colorDialog.ShowDialog();
      if (result == DialogResult.OK) {
        this.Value = colorDialog.Color;
      }
    }
  }
}
