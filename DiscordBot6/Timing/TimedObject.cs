﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscordBot6.Timing {
    public abstract class TimedObject {
        public virtual DateTime? Expiry {
            get => expiry;
            set {
                expiry = value;
                timer?.Dispose();

                if (value != null) {
                    if (value < DateTime.UtcNow) {
                        OnExpiry(null);
                    }

                    else {
                        timer = new Timer(OnExpiry, null, value.Value - DateTime.UtcNow, TimeSpan.FromMilliseconds(-1));
                    }
                }
            } 
        }

        private DateTime? expiry;

        private Timer timer;

        public abstract void OnExpiry(object state);
    }
}