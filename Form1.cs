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
        private readonly OptimizedPicking _optimizedPicking;

        private CancellationTokenSource _ctsDefault;
        private CancellationTokenSource _ctsOptimized;
        public Form1(
            TaskService taskService,
            WarehouseTopology topology,
            DrawingService drawingService,
            DefaultPicking defaultPicking,
            OptimizedPicking optimizedPicking)
        {
            _taskService = taskService;
            _topology = topology;
            _drawingService = drawingService;
            
            _defaultPicking = defaultPicking;
            _optimizedPicking = optimizedPicking;
            
            InitializeComponent();
            
            WarehouseFloor1PictureBox.Image = _drawingService.DrawWarehouse();

            _ctsDefault = new();
            _ctsOptimized = new();
            
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

            await Task.Delay(1500);
            
            Task.Run(() =>
            {
                _taskService.GenerateTasks(35, 3, 6, 20_000, _ctsDefault);
            });
            
            Task.Run(() =>
            {
                _optimizedPicking.StartProcess(_ctsOptimized);
            });
        }

        private async void DefaultButton_Click(object sender, EventArgs e)
        {
            DefaultButton.Enabled = false;
            OptimizedButton.Enabled = true;
            await _ctsOptimized.CancelAsync();
            _ctsOptimized = new CancellationTokenSource();
            
            await Task.Delay(1500);
            
            Task.Run(() =>
            {
                _taskService.GenerateTasks(35, 3, 6, 20_000, _ctsDefault);
            });
            
            Task.Run(() =>
            {
                _defaultPicking.StartProcess(_ctsDefault);
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
            WarehouseFloor1PictureBox.Image = _drawingService.Bitmap;
        }
    }
}
