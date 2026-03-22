using Microsoft.AspNetCore.Components;
using System;

namespace Feniks.Web
{
    public class NotificationService
    {
        public event Action<string, string>? OnShow;
        public event Action? OnHide;
        
        // Добавляем событие для уведомления об изменении данных
        public event Action? OnDataChanged;
        
        // Метод для уведомления об изменении данных (вызывается из EstimateEditor)
        public void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }

        public void ShowSuccess(string message)
        {
            OnShow?.Invoke("success", message);
        }

        public void ShowError(string message)
        {
            OnShow?.Invoke("error", message);
        }

        public void ShowInfo(string message)
        {
            OnShow?.Invoke("info", message);
        }

        public void ShowWarning(string message)
        {
            OnShow?.Invoke("warning", message);
        }

        public void Hide()
        {
            OnHide?.Invoke();
        }
    }
}