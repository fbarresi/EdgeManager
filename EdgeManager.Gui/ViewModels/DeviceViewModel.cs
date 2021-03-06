﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EdgeManager.Gui.Design;
using EdgeManager.Gui.Views;
using EdgeManager.Interfaces.Commons;
using EdgeManager.Interfaces.Extensions;
using EdgeManager.Interfaces.Models;
using EdgeManager.Interfaces.Services;
using ReactiveUI;

namespace EdgeManager.Gui.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        private readonly IAzureService azureService;
        private readonly ISelectionService<IoTHubInfo> ioTHubInfoSelectionService;
        private readonly ISelectionService<IoTDeviceInfo> ioTDeviceSelectionService;
        private readonly IViewModelFactory viewModelFactory;
        private IoTDeviceInfo selectedIoTDeviceInfo;
        private IoTDeviceInfo[] ioTDeviceInfos;
        private bool loading;
        private string hubName;


        public ReactiveCommand<Unit, Unit> AddNewDeviceCommand { get; set; }
        public ReactiveCommand<Unit, Unit> DeleteSelectedDeviceCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ReloadCommand { get; set; }
        public ReactiveCommand<IoTDeviceInfo, Unit> MonitorDevice { get; set; }

        public DeviceViewModel(IAzureService azureService, 
            ISelectionService<IoTHubInfo> ioTHubInfoSelectionService, ISelectionService<IoTDeviceInfo> ioTDeviceSelectionService, 
            IViewModelFactory viewModelFactory)
        {
            this.azureService = azureService;
            this.ioTHubInfoSelectionService = ioTHubInfoSelectionService;
            this.ioTDeviceSelectionService = ioTDeviceSelectionService;
            this.viewModelFactory = viewModelFactory;
        }

        public override void Initialize()
        {
            AddNewDeviceCommand = ReactiveCommand.CreateFromTask(CreateNewDevice, ioTHubInfoSelectionService.SelectedObject.Select(iotHub => iotHub != null))
                    .AddDisposableTo(Disposables);
            DeleteSelectedDeviceCommand = ReactiveCommand.CreateFromTask(DeleteSelectedDevice, ioTDeviceSelectionService.SelectedObject.Select(ioTDeviceInfos => ioTDeviceInfos != null))
                    .AddDisposableTo(Disposables);
           
            this.WhenAnyValue(vm => vm.SelectedIoTDeviceInfo)
                .Subscribe(x => ioTDeviceSelectionService.Select(x))
                .AddDisposableTo(Disposables);

            ioTHubInfoSelectionService.SelectedObject
                .Where(iotHub => iotHub != null)
                .ObserveOnDispatcher()
                .Do(_ => Loading = true)
                .Do(devices => IoTDeviceInfos = new IoTDeviceInfo[]{})
                .Do(i => this.hubName = i.Name)
                .Subscribe()
                .AddDisposableTo(Disposables);
                
            ioTHubInfoSelectionService.SelectedObject
                .Where(iotHub => iotHub != null)
                .SelectMany(iothub => azureService.GetIoTDevices(iothub.Name))
                .ObserveOnDispatcher()
                .Do(devices => IoTDeviceInfos = devices)
                .Do(_ => Loading = false)
                .LogAndRetryAfterDelay(Logger, TimeSpan.FromSeconds(1), "Error while retrieving devices information")
                .Subscribe()
                .AddDisposableTo(Disposables);

            ReloadCommand = ReactiveCommand.CreateFromTask(Reload, ioTHubInfoSelectionService.SelectedObject.Select(iotHub => iotHub != null))
                .AddDisposableTo(Disposables);

            MonitorDevice = ReactiveCommand.CreateFromTask<IoTDeviceInfo, Unit>(OpenMonitorDevice)
                .AddDisposableTo(Disposables);
        }

        private Task<Unit> OpenMonitorDevice(IoTDeviceInfo device)
        {
            Logger.Debug($"Opening monitor window for {device.DeviceId}...");
            var window = new MonitorDeviceWindow();
            var viewModel = viewModelFactory.CreateViewModel<IoTDeviceInfo, MonitorDeviceViewModel>(device);
            viewModel.AddDisposableTo(Disposables);
            window.DataContext = viewModel;
            window.Show();
            Logger.Debug($"Opend Dialog Window to add New Device Name");
            return Task.FromResult(Unit.Default);
        }

        private async Task<Unit> CreateNewDevice()
        {
            using (var disposables = new CompositeDisposable())
            {
                try
                {
                    Logger.Debug($"User pressed 'Add New Device'");
                    var dialog = new InputWindowDevice();
                    var viewModel = viewModelFactory.CreateViewModel<InputWindowDeviceViewModel>();
                    viewModel.AddDisposableTo(disposables);
                    viewModel.Window = dialog;
                    dialog.DataContext = viewModel;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    var finished = dialog.ShowDialog();
                    Logger.Debug($"Opend Dialog Window to add New Device Name");

                    if (viewModel.CanAddDevice)
                    {
                        var newDeviceName = viewModel.NewDeviceName;
                        Logger.Debug($"User entered Name:'{viewModel.NewDeviceName}' and pressed 'Create New Device' Button");
                        await azureService.CreateNewDevice(hubName, newDeviceName);
                        Logger.Debug($"Command was send with AzureCli");
                        await Reload();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error while creating new device", e);
                }
            }

            return Unit.Default;
        }

        public async Task<Unit> DeleteSelectedDevice()
        {
            using (var disposables = new CompositeDisposable())
            {
                try
                {
                    Logger.Debug($"User pressed 'Delete Selcted Device'");
                    var dialog = new ConfirmWindow();
                    var viewModel = viewModelFactory.CreateViewModel<ConfirmWindowViewModel>();
                    viewModel.AddDisposableTo(disposables);
                    viewModel.Window = dialog;
                    dialog.DataContext = viewModel;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    var finished = dialog.ShowDialog();
                    Logger.Debug($"Opend Dialog Window to Confirm if User want to delete the selected Device");

                    if (viewModel.DeleteDevice)
                    {
                        Logger.Debug($"User Confirm with 'Yes Delete'");
                        await azureService.DeleteSelectedDevice(hubName, selectedIoTDeviceInfo.DeviceId);
                        Logger.Debug($"Selected Device was deleted");
                        await Reload();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error in Reactive command", e);
                }
                return Unit.Default;
            }
        }

        public bool Loading
        {
            get => loading;
            set
            {
                if (value == loading) return;
                loading = value;
                raisePropertyChanged();
            }
        }

        public async Task<Unit> Reload()
        {
            try
            {
                Loading = true;
                Logger.Debug($"Start reloading -IoT / Edge Devices-");
                IoTDeviceInfos = await azureService.GetIoTDevices(hubName, reload: true);
                Logger.Debug($"-IoT / Edge Devices- was reloaded");
                Loading = false;
            }
            catch (Exception e)
            {
                Logger.Error("Error in Reactive command", e);
            }
            return Unit.Default;
        }

        public IoTDeviceInfo[] IoTDeviceInfos
        {
            get => ioTDeviceInfos;
            private set
            {
                if (Equals(value, ioTDeviceInfos)) return;
                ioTDeviceInfos = value;
                raisePropertyChanged();
            }
        }

        public IoTDeviceInfo SelectedIoTDeviceInfo
        {
            get => selectedIoTDeviceInfo;
            set
            {
                if (Equals(value, selectedIoTDeviceInfo)) return;
                selectedIoTDeviceInfo = value;
                raisePropertyChanged();
            }
        }
    }

    internal class DesignDeviceViewModel : DeviceViewModel
    {
        public DesignDeviceViewModel() : base(new DesignAzureService(), new DesignIoTHubSelectionService(), new DesignDeviceSelectionService(), new ViewModelLocator())
        {
        }
    }
}
