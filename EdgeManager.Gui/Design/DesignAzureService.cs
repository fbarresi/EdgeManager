﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdgeManager.Interfaces.Models;
using EdgeManager.Interfaces.Services;
using Newtonsoft.Json.Linq;

namespace EdgeManager.Gui.Design
{
    internal class DesignAzureService : IAzureService
    {
        public IObservable<JsonCommand> JsonCommands { get; } = Observable.Never<JsonCommand>();

        public bool Loading => throw new NotImplementedException();

        public Task<IoTHubInfo[]> GetIoTHubs(bool reload = false)
        {
            return Task.FromResult(new[]
            {
                new IoTHubInfo {Name = "Hub1", Properties = new IoTHubProperties()},
                new IoTHubInfo {Name = "Hub2", Properties = new IoTHubProperties()},
                new IoTHubInfo {Name = "Hub3", Properties = new IoTHubProperties()}
            });
        }

        public Task<IoTDeviceInfo[]> GetIoTDevices(string hubName, bool reload = false)
        {
            return Task.FromResult(new[]
            {
                new IoTDeviceInfo {DeviceId = "Device1", HubName = hubName},
                new IoTDeviceInfo {DeviceId = "Device2", HubName = hubName},
                new IoTDeviceInfo {DeviceId = "Device3", HubName = hubName}
            });
        }

        public Task<IoTModuleIdentityInfo[]> GetIoTModules(string hubName, string deviceId, bool reload = false)
        {
            return Task.FromResult(new[]
            {
                new IoTModuleIdentityInfo {ModuleId = "Module1"},
                new IoTModuleIdentityInfo {ModuleId = "Module2"},
                new IoTModuleIdentityInfo {ModuleId = "Module3"},
            });
        }

        public Task<ModuleTwin> GetIoTModelTwinProperties(string hubName, string deviceId, string moduleId)
        {
            throw new NotImplementedException();
        }

        public Task<IoTDirectMethodReply> CallMethod(string method, string hubName, string deviceId, string moduleId, DirectMethodPayloadBase payload)
        {
            throw new NotImplementedException();
        }

        public Task Login(CancellationToken token)
        {
            return Task.FromResult(Unit.Default);
        }

        public Task Logout()
        {
            return Task.FromResult(Unit.Default);
        }

        public Task<AzureAccountInfo> GetAccount()
        {
            return Task.FromResult(new AzureAccountInfo()
                {Name = "MyDummy Subscription", User = new AzureUser() {Name = "Dev", Type = "User"}});
        }

        public Task CreateNewDevice(string hubName, string newDeviceName)
        {
            return Task.FromResult(Unit.Default);
        }

        public Task DeleteSelectedDevice(string hubName, string deviceId)
        {
            return Task.FromResult(Unit.Default);
        }

        public Task<bool> CheckCli()
        {
            return Task.FromResult(true);
        }

        public ICommandHandler ObserveDeviceMessages(string hubName, string deviceId)
        {
            return new DesignCommandHandler();
        }

        public ICommandHandler MonitorDevice(string hubName, string deviceId)
        {
            return new DesignCommandHandler();
        }
    }
}
