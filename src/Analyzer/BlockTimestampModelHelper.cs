
using System;
using System.Collections.Generic;

using OxyPlot;
using OxyPlot.Axes;

namespace Analyzer
{



	public sealed class BlockTimestampModelHelper
		: BasePlotModelHelper
	{
		private DateTimeAxis _xAxis;
		private LinearAxis _yAxis;

		protected override Axis CreateXAxis()
		{
			_xAxis = new DateTimeAxis()
			{
				Title = "Время"
			};
			return _xAxis;
		}

		protected override Axis CreateYAxis()
		{
			_yAxis = new LinearAxis()
			{
				Title = "Номер блока"
			};
			return _yAxis;
		}


		public void LoadData(string name, IEnumerable<BlockTimestampPoint> points)
		{
			lock (PlotModel.SyncRoot)
			{
				OxyPlot.Series.LineSeries lineSeries = new()
				{
					Title = name
				};
				foreach (var point in points)
				{
					var x = DateTimeAxis.ToDouble(point.DateTime);
					lineSeries.Points.Add(new DataPoint(x, point.BlockNumber));
				}
				PlotModel.Series.Add(lineSeries);
			}
		}


		public readonly struct BlockTimestampPoint
		{
			public readonly DateTime DateTime;
			public readonly long BlockNumber;

			public BlockTimestampPoint(DateTime dateTime, long blockNumber)
			{
				DateTime = dateTime;
				BlockNumber = blockNumber;
			}
		}
	}
}
