using FF.Drawing;

namespace FF
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            var drawingService = new DrawingService(WarehousePictureBox.Width, WarehousePictureBox.Height);
            WarehousePictureBox.Image = drawingService.DrawWarehouse();
        }

        private void OptimizedButton_Click(object sender, EventArgs e)
        {

        }

        private void DefaultButton_Click(object sender, EventArgs e)
        {

        }

        private void AcceptDelayButton_Click(object sender, EventArgs e)
        {
            /*
                if (int.TryParse(msDelayTextBox.Text, out int delay))
                {
                    msDelay = delay;
                    delayLabel.Text = "Done";
                    await Task.Delay(1500);
                    delayLabel.Text = "";
                }
             */
        }

        private void DelayTextBox_TextChanged(object sender, EventArgs e)
        {
            //TODO проверка ввода, но не менять задержку
        }
    }
}