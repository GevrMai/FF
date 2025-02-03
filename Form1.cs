using Application.Services;
using Domain;
using Domain.Interfaces;

namespace FF
{
    public partial class Form1 : Form
    {
        private readonly ITaskService _taskService;
        private readonly IDrawingService _drawingService;
        private readonly IPicking _defaultPicking;
        private readonly IPicking _optimizedPicking;

        private CancellationTokenSource _ctsDefault;
        private CancellationTokenSource _ctsOptimized;
        public Form1(
            ITaskService taskService,
            IDrawingService drawingService,
            IEnumerable<IPicking> pickingServices)
        {
            _taskService = taskService;
            _drawingService = drawingService;
            
            _defaultPicking = pickingServices.Single(x => x.GetType() == typeof(DefaultPicking));
            _optimizedPicking = pickingServices.Single(x => x.GetType() == typeof(OptimizedPicking));
            
            InitializeComponent();
            
            WarehousePictureBox.Image = (Bitmap)_drawingService.DrawWarehouse();

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
                _taskService.GetTasks(
                    Consts.TasksCountPerBatch,
                    Consts.NumberOfBatches,
                    Consts.MaxWeightOfTaskKg,
                    Consts.DelayBetweenBatchesSeconds,
                    GenerationsCountLabel,
                    _ctsDefault.Token);
            });

            Task.Run(() =>
            {
                _optimizedPicking.StartProcess(_ctsOptimized);
            });

            Task.Run(async () =>
            {
                while (!_ctsOptimized.IsCancellationRequested)
                {
                    TasksInQueueCountLabel.Text = _taskService.GetTasksInQueueCount().ToString();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));   
                }
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
                _taskService.GetTasks(
                    Consts.TasksCountPerBatch,
                    Consts.NumberOfBatches,
                    Consts.MaxWeightOfTaskKg,
                    Consts.DelayBetweenBatchesSeconds, 
                    GenerationsCountLabel,
                    _ctsOptimized.Token);
            });
            
            Task.Run(() =>
            {
                _defaultPicking.StartProcess(_ctsDefault);
            });

            Task.Run(async () =>
            {
                while (!_ctsDefault.IsCancellationRequested)
                {
                    TasksInQueueCountLabel.Text = _taskService.GetTasksInQueueCount().ToString();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));   
                }
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
            WarehousePictureBox.Image = (Bitmap)_drawingService.GetBitmap();
        }
    }
}
