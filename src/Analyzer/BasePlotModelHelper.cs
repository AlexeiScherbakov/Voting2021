
using System;
using System.Collections.Generic;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;

namespace Analyzer
{
	public abstract class BasePlotModelHelper
	{
		private readonly PlotModel _plotModel;

		public BasePlotModelHelper()
		{
			_plotModel = new PlotModel();
			var l = new Legend
			{
				LegendPlacement = LegendPlacement.Inside,
				LegendPosition = LegendPosition.RightTop,
				LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
				LegendBorder = OxyColors.Black,
			};
			_plotModel.Legends.Add(l);

			var xAxis = CreateXAxis();
			xAxis.Position = AxisPosition.Bottom;
			xAxis.Key = XAxisKey;
			_plotModel.Axes.Add(xAxis);

			var yAxis = CreateYAxis();
			yAxis.Position = AxisPosition.Left;
			yAxis.Key = YAxisKey;
			_plotModel.Axes.Add(yAxis);
		}

		public PlotModel PlotModel
		{
			get { return _plotModel; }
		}


		public const string XAxisKey = "x_axis";
		public const string YAxisKey = "y_axis";

		protected abstract Axis CreateXAxis();

		protected abstract Axis CreateYAxis();
	}
}
