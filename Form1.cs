using FF.Drawing;
using FF.Picking;
using FF.TasksData;
using FF.WarehouseData;

namespace FF
{
    public partial class Form1 : Form
    {
        private readonly TaskService _taskService;
        private readonly WarehouseTopology _topology;
        private readonly DrawingService _drawingService;
        private readonly DefaultPicking _defaultPicking;

        private CancellationTokenSource _ctsDefault;
        private CancellationTokenSource _ctsOpt;
        public Form1(
            TaskService taskService,
            WarehouseTopology topology,
            DrawingService drawingService,
            DefaultPicking defaultPicking)
        {
            _taskService = taskService;
            _topology = topology;
            _drawingService = drawingService;
            
            _defaultPicking = defaultPicking;
            
            InitializeComponent();
            
            WarehousePictureBox.Image = _drawingService.DrawWarehouse();

            _ctsDefault = new();
            _ctsOpt = new();
            
            _drawingService.BitmapChanged += DrawingService_BitmapChanged;
            
            StatusLabel.Text = "Ready";
            StatusLabel.BackColor = Color.Green;
        }

        private async void OptimizedButton_Click(object sender, EventArgs e)    // async void is bad :)
        {
            OptimizedButton.Enabled = false;
            DefaultButton.Enabled = true;
            await _ctsDefault.CancelAsync();
            _ctsDefault = new CancellationTokenSource();
        }

        private async void DefaultButton_Click(object sender, EventArgs e)
        {
            DefaultButton.Enabled = false;
            OptimizedButton.Enabled = true;
            await _ctsOpt.CancelAsync();
            _ctsOpt = new CancellationTokenSource();
            
            _taskService.GenerateTasks(5, 1, 5, 1, _ctsDefault);
            
            Console.WriteLine(_taskService.TasksQueue.Count);   // TODO убрать
            
            
            Task.Run(() =>
            {
                _defaultPicking.StartProccess(_ctsDefault);
            });
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
        
        private void DrawingService_BitmapChanged(object sender, EventArgs e)
        {
            // Установите Bitmap для PictureBox
            WarehousePictureBox.Image = _drawingService.Bitmap;
        }
    }
}
