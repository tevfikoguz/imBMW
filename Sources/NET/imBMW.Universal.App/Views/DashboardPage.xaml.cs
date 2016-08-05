﻿using imBMW.Diagnostics.DME;
using imBMW.iBus;
using imBMW.Universal.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace imBMW.Universal.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DashboardPage : ExtendedPage
    {
        private List<GaugeWatcher> gauges;

        public List<GaugeWatcher> Gauges
        {
            get
            {
                if (gauges == null)
                {
                    gauges = GaugeWatcher.FromSettingsList(new List<GaugeSettings>
                    {
                        new GaugeSettings { Name = "Oil", Field = "OilTemp", Format = "N0", Dimention = "Celsius", MinValue = 0, MaxValue = 150, MinRed = 60, MinYellow = 75, MaxYellow = 95, MaxRed = 105 },
                        new GaugeSettings { Name = "Voltage", Field = "VoltageBattery", Format = "F1", Dimention = "Volts" },
                        new GaugeSettings { Name = "Coolant", Field = "CoolantTemp" },
                        new GaugeSettings { Name = "Radiator" },
                    });
                }
                return gauges;
            }
        }

        public DashboardPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Manager.AfterMessageReceived += Manager_AfterMessageReceived;


            var av = new MS43JMGAnalogValues();
            av.OilTemp = 95.5;
            av.VoltageBattery = 14.1;
            av.CoolantTemp = 93.1;
            av.CoolantRadiatorTemp = 90.3;
            Gauges.ForEach(g => g.Update(av));

            // TODO don't do on second visit
            testTimer = new DispatcherTimer();
            testTimer.Interval = TimeSpan.FromMilliseconds(1);
            testTimer.Tick += TestTimer_Tick;
            testTimer.Start();            
        }

        DispatcherTimer testTimer;
        double testTimerTicks = 0;

        private void TestTimer_Tick(object sender, object e)
        {
            if (testTimerTicks > 200)
            {
                testTimer.Stop();
                testTimer = null;
                return;
            }
            foreach (var g in Gauges)
            {
                TestGauge(g, testTimerTicks);
                if (g.SecondaryWatcher != null)
                {
                    TestGauge(g.SecondaryWatcher, testTimerTicks);
                }
            }
            testTimerTicks += 3;
        }

        void TestGauge(GaugeWatcher g, double percent)
        {
            if (percent > 100)
            {
                percent = 200 - percent;
            }
            if (percent < 3)
            {
                percent = 0;
            }
            var value = g.Settings.MinValue + (g.Settings.MaxValue - g.Settings.MinValue) * percent / 100;
            g.RawValue = value;
        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Manager.AfterMessageReceived -= Manager_AfterMessageReceived;
        }

        private void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            if (MS43AnalogValues.CanParse(e.Message))
            {
                var av = new MS43JMGAnalogValues();
                av.Parse(e.Message);
                Gauges.ForEach(g => g.Update(av));
            }
        }
    }
}
