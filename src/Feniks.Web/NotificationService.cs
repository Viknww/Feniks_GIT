using System;

namespace Feniks.Web
{
    public class NotificationService
    {
        public event Action? OnChange;

        public void NotifyDataChanged()
        {
            OnChange?.Invoke();
        }
    }
}