using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.Wpf;
using DateTimeAxis = OxyPlot.Axes.DateTimeAxis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using System.IO;
using System.Windows;
using System.Threading;
using System.Linq;

namespace EconomyBot
{
    class RandUtil
    {       
        public static void ActivityGraph(string filename, List<double> dailyMoney, ISocketMessageChannel ch)
        {
            TimeSpan interval = new TimeSpan(1, 0, 0, 0, 0);
            List<DateTime> gridMarks = new List<DateTime>();

            LineSeries s = new LineSeries
            {
                StrokeThickness = 5,
                Color = OxyColors.White
            };

            for (int i = 0; i <= dailyMoney.Count; i++) {
                gridMarks.Add(DateTime.Now.Subtract(new TimeSpan(7*i, 0, 0, 0, 0)));
                if (i != dailyMoney.Count) {
                    s.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Now.Subtract(new TimeSpan(7 * i, 0, 0, 0, 0))), dailyMoney[i]));
                }
            }
            PlotModel plot = new PlotModel
            {
                PlotAreaBorderColor = OxyColors.White,
                PlotAreaBorderThickness = new OxyThickness(2)
            };
            plot.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time",
                MajorStep = DateTimeAxis.ToDouble(interval),
                ExtraGridlines = gridMarks.Select(d => DateTimeAxis.ToDouble(d)).ToArray(),
                ExtraGridlineColor = OxyColor.FromArgb(120, 76, 255, 0),
                ExtraGridlineThickness = 1.5,
                AxislineColor = OxyColors.White,
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White
            });
            LinearAxis l = new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = "M O N E Y",
                ExtraGridlineColor = OxyColor.FromArgb(120, 76, 255, 0),
                ExtraGridlineThickness = 1.5,
                TitleColor = OxyColors.White,
                AxislineColor = OxyColors.White,
                TicklineColor = OxyColors.White
            };
            Console.WriteLine(dailyMoney.Max());
            if (dailyMoney.Max() <= int.MaxValue / 1000 && (int)dailyMoney.Max() <= int.MaxValue / 10000)
            {
                l.ExtraGridlines = Enumerable.Range(1, (int)dailyMoney.Max()).Select(i => (double)i * 10000).ToArray();
            }
            else
            {
                l.ExtraGridlines = Enumerable.Range(1, int.MaxValue / 10000).Select(i => (double)i * 10000).ToArray();
            }
            plot.Axes.Add(l);
            plot.Series.Add(s);

            Thread thr = new Thread(new ThreadStart(delegate {
                try { 
                    
                    PngExporter.Export(plot, filename + ".png", 2000, 750, OxyColors.Black);                     
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }));
            thr.SetApartmentState(ApartmentState.STA);
            thr.Start();
            while (thr.IsAlive) {
                Thread.Sleep(100);
            }
        }
    }
}
