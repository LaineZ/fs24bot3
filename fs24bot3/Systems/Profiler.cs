using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using fs24bot3.Models;

namespace fs24bot3.Systems
{
    public class Profiler
    {
        public int WindowSize = 50;
        private Dictionary<string, (List<long>, Stopwatch)> Metrics = new Dictionary<string, (List<long>, Stopwatch)>();
        private List<int> MemoryUsage = new List<int>();

        public void AddMetric(string metric)
        {
            Metrics.Add(metric, (new List<long>(), new Stopwatch()));
        }

        public void BeginMeasure(string metric)
        {
            Metrics[metric].Item2.Start();
        }

        public void EndMeasure(string measure)
        {
            Metrics[measure].Item2.Stop();
            Metrics[measure].Item1.Add(Metrics[measure].Item2.ElapsedMilliseconds);
            Metrics[measure].Item2.Reset();
            if (Metrics[measure].Item1.Count > WindowSize)
            {
                Metrics[measure].Item1.RemoveAt(0);
            }
        }

        public float GetMeasureLast(string metric)
        {
            return !Metrics[metric].Item1.Any() ? 0 : Metrics[metric].Item1.Last();
        }

        public int GetMeasureAvg(string metric)
        {
            if (!Metrics[metric].Item1.Any()) { return 0; }
            return (int)Metrics[metric].Item1.Average();
        }

        public float GetMeasureMin(string metric)
        {
            return !Metrics[metric].Item1.Any() ? 0 : Metrics[metric].Item1.Min();
        }

        public float GetMeasureMax(string metric)
        {
            return !Metrics[metric].Item1.Any() ? 0 : Metrics[metric].Item1.Max();
        }

        public string FmtMetric(float metric)
        {
            if (metric > 1000)
            {
                return $"[red]{metric / 1000} s[r]";
            }
            else if (metric > 60000)
            {
                return $"[red]{metric / 60000} m[r]";
            }
            else
            {
                return $"{metric} ms";
            }
        }

        public string Fmt(string metric)
        {
            if (Metrics[metric].Item1.Any())
            {
                return string.Format("{0,15} │ last {1,8} │ avg {2,8} │ min {3,8} │ max {4,8}", metric,
                    FmtMetric(GetMeasureLast(metric)),
                    FmtMetric(GetMeasureAvg(metric)),
                    FmtMetric(GetMeasureMin(metric)),
                    FmtMetric(GetMeasureMax(metric)));
            }

            return $"{0,15} │ last {0,8} │ avg {0,8} │ min {0,8} │ max {0,8}";
        }

        public string FmtAll()
        {
            var sb = new StringBuilder();
            foreach (var metric in Metrics)
            {
                if (metric.Value.Item1.Any())
                {
                    sb.Append(Fmt(metric.Key));
                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}