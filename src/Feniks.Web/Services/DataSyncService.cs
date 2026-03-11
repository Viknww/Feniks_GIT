using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Feniks.Web.Services
{
    public class DataSyncService
    {
        // События для разных типов данных
        public event Func<int, Task>? OnEstimateChanged;
        public event Func<int, Task>? OnObjectChanged;
        public event Func<int, Task>? OnReferenceChanged;
        public event Func<int, Task>? OnContractorChanged;
        public event Func<Task>? OnAnyDataChanged;

        // Универсальный словарь для хранения любых подписок (на будущее)
        private readonly Dictionary<string, List<Func<object, Task>>> _customEvents = new();

        public async Task NotifyEstimateChanged(int estimateId)
        {
            Console.WriteLine($"🔄 DataSync: смета {estimateId} изменена");
            if (OnEstimateChanged != null)
                await OnEstimateChanged.Invoke(estimateId);
            await NotifyAnyDataChanged();
        }

        public async Task NotifyObjectChanged(int objectId)
        {
            Console.WriteLine($"🔄 DataSync: объект {objectId} изменен");
            if (OnObjectChanged != null)
                await OnObjectChanged.Invoke(objectId);
            await NotifyAnyDataChanged();
        }

        public async Task NotifyReferenceChanged(int referenceId)
        {
            Console.WriteLine($"🔄 DataSync: справочник {referenceId} изменен");
            if (OnReferenceChanged != null)
                await OnReferenceChanged.Invoke(referenceId);
            await NotifyAnyDataChanged();
        }

        public async Task NotifyContractorChanged(int contractorId)
        {
            Console.WriteLine($"🔄 DataSync: контрагент {contractorId} изменен");
            if (OnContractorChanged != null)
                await OnContractorChanged.Invoke(contractorId);
            await NotifyAnyDataChanged();
        }

        private async Task NotifyAnyDataChanged()
        {
            if (OnAnyDataChanged != null)
                await OnAnyDataChanged.Invoke();
        }

        // Для расширения - подписка на кастомные события
        public void Subscribe(string eventName, Func<object, Task> handler)
        {
            if (!_customEvents.ContainsKey(eventName))
                _customEvents[eventName] = new List<Func<object, Task>>();
            _customEvents[eventName].Add(handler);
        }

        public async Task NotifyCustom(string eventName, object data)
        {
            if (_customEvents.ContainsKey(eventName))
            {
                foreach (var handler in _customEvents[eventName])
                {
                    await handler(data);
                }
            }
        }
    }
}