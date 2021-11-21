using System;
using System.Timers;

namespace Lottie.Timing {
    public abstract class TimedObject : IDisposable {
        public virtual DateTime? Expiry {
            get => expiry;
            set {
                expiry = value;

                if (value.HasValue) {
                    if (value < DateTime.UtcNow) {
                        Timer_Elapsed(null, null);
                    }

                    else {
                        timer.Interval = (value.Value - DateTime.UtcNow).TotalMilliseconds;
                        timer.Enabled = true;
                    }
                }

                else {
                    timer.Enabled = false;
                }
            } 
        }

        public virtual bool Expired => expiry.HasValue && expiry.Value < DateTime.UtcNow;

        private DateTime? expiry;
        private readonly Timer timer;

        public TimedObject() {
            timer = new Timer() {
                AutoReset = false
            };

            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs eventArgs) {
            OnExpiry();
            timer.Enabled = false;
        }

        public virtual void Dispose() {
            timer?.Dispose();
        }

        public abstract void OnExpiry();
    }
}
