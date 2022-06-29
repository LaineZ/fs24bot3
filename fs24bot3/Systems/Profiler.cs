using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace fs24bot3.Systems
{
    public class Profiler
    {
        public int WindowSize = 50;
        private Dictionary<string, (List<long>, Stopwatch)> Metrics = new Dictionary<string, (List<long>, Stopwatch)>();

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
            return (float)Metrics[metric].Item1.Last();
        }

        public float GetMeasureAvg(string metric)
        {
            return (float)Metrics[metric].Item1.Average();
        }

        public float GetMeasureMin(string metric)
        {
            return Metrics[metric].Item1.Min();
        }

        public float GetMeasureMax(string metric)
        {
            return Metrics[metric].Item1.Max();
        }

        public string Fmt(string metric)
        {
            return $"{metric}:	last:	{GetMeasureLast(metric)} ms |	avg: {GetMeasureAvg(metric)} ms	|	min: {GetMeasureMin(metric)}	|	max: {GetMeasureMax(metric)}";
        }

        public string FmtAll()
        {
            var sb = new StringBuilder();
            foreach (var metric in Metrics)
            {
                sb.Append(Fmt(metric.Key));
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}