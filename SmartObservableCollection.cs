using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Byteopia.Music.GoogleMusicAPI
{
    public class SmartObservableCollection<T> : ObservableCollection<T>
    {
        public IEnumerable<T> RealNewItems
        {
            get;
            set;
        }

        public SmartObservableCollection(Action<Action> dispatchingAction = null)
            : base()
        {
            iSuspendCollectionChangeNotification = false;
            if (dispatchingAction != null)
                iDispatchingAction = dispatchingAction;
            else
                iDispatchingAction = a => a();
        }

        private bool iSuspendCollectionChangeNotification;
        private Action<Action> iDispatchingAction;


        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!iSuspendCollectionChangeNotification)
            {
                using (IDisposable disposeable = this.BlockReentrancy())
                {
                    iDispatchingAction(() =>
                    {
                        base.OnCollectionChanged(e);
                    });
                }
            }
        }

        public void SuspendCollectionChangeNotification()
        {
            iSuspendCollectionChangeNotification = true;
        }

        public void ResumeCollectionChangeNotification()
        {
            iSuspendCollectionChangeNotification = false;
        }


        public void AddRange(IEnumerable<T> items)
        {
            this.SuspendCollectionChangeNotification();
            try
            {
                foreach (var i in items)
                {
                    base.InsertItem(base.Count, i);
                }
            }
            finally
            {
                this.RealNewItems = items;
                this.ResumeCollectionChangeNotification();
                var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                this.OnCollectionChanged(arg);
            }
        }
    }
}