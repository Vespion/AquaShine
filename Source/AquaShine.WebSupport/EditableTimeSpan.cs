using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AquaShine.WebSupport
{
    public class EditableTimeSpan : INotifyPropertyChanged
    {
        TimeSpan _timespan;

        public TimeSpan TimeSpan
        {
            get { return _timespan; }
            set { _timespan = value; FireAll(); }
        }
        public int Hours
        {
            get { return TimeSpan.Hours; }
            set
            {
                if (TimeSpan.Hours != value)
                {
                    TimeSpan = new TimeSpan(value, TimeSpan.Minutes, TimeSpan.Seconds);
                    FireAll();
                }
            }
        }
        public int Minutes
        {
            get { return TimeSpan.Minutes; }
            set
            {
                if (TimeSpan.Minutes != value)
                {
                    TimeSpan = new TimeSpan(TimeSpan.Hours, value, TimeSpan.Seconds);
                    FireAll();
                }
            }
        }
        public int Seconds
        {
            get { return TimeSpan.Seconds; }
            set
            {
                if (TimeSpan.Seconds != value)
                {
                    TimeSpan = new TimeSpan(TimeSpan.Hours, TimeSpan.Minutes, value);
                    FireAll();
                }
            }
        }

        public int MiliSeconds
        {
            get { return TimeSpan.Milliseconds; }
            set
            {
                if (TimeSpan.Seconds != value)
                {
                    TimeSpan = new TimeSpan(days: 0, hours: TimeSpan.Hours, minutes: TimeSpan.Minutes, seconds: TimeSpan.Seconds, milliseconds: value);
                    FireAll();
                }
            }
        }

        public double TotalSeconds
        {
            get { return TimeSpan.TotalSeconds; }
            set
            {
                TimeSpan = TimeSpan.FromSeconds(value);
                FireAll();
            }
        }
        public static explicit operator TimeSpan(EditableTimeSpan ets)
        {
            return ets.TimeSpan;
        }
        void FireAll()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, MinutesArgs);
                PropertyChanged(this, HoursArgs);
                PropertyChanged(this, TotalSecondsArgs);
                PropertyChanged(this, SecondsArgs);
                PropertyChanged(this, MiliSecondsArgs);
            }
        }
        public static explicit operator EditableTimeSpan(TimeSpan ets)
        {
            return new EditableTimeSpan(ets);
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private PropertyChangedEventArgs MinutesArgs = new PropertyChangedEventArgs("Minutes");
        private PropertyChangedEventArgs TotalSecondsArgs = new PropertyChangedEventArgs("TotalSeconds");
        private PropertyChangedEventArgs SecondsArgs = new PropertyChangedEventArgs("Seconds");
        private PropertyChangedEventArgs MiliSecondsArgs = new PropertyChangedEventArgs("MiliSeconds");
        private PropertyChangedEventArgs HoursArgs = new PropertyChangedEventArgs("Hours");

        public override string ToString()
        {
            return TimeSpan.ToString();
        }
        public string ToString(string format)
        {
            return TimeSpan.ToString(format);
        }
        public EditableTimeSpan()
        {
            _timespan = new TimeSpan();
        }
        public EditableTimeSpan(TimeSpan timeSpan)
        {
            _timespan = timeSpan;
        }
    }
}
