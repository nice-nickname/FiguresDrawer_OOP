﻿using FiguresDrawer.App.Core;
using FiguresDrawer.Model.Factories;
using FiguresDrawer.Model.Structures;
using FiguresDrawer.Presenter.Adapter;
using FiguresDrawer.Presenter.Drawing;
using FiguresDrawer.Presenter.Events;
using FiguresDrawer.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FiguresDrawer.Presenter
{
	public class FiguresCreatorPresenter : IPresenter
	{
		private IFiguresCreatorView _view;

		private ListBox.ObjectCollection _outFigures;

		private ListBox.ObjectCollection _figuresBuffer;
		private ListBox.ObjectCollection _pointsBuffer;

		public event Action<IPresenter, EventArgs> SendData;

		public FiguresCreatorPresenter(IFiguresCreatorView view)
		{
			_view = view;

			_figuresBuffer = _view.FiguresBuffer;
			_pointsBuffer = _view.PointsBuffer;

			_view.ReadFromFileButton_Click	+= View_OnReadFromFileButton_Click;
			_view.ClearPointsButton_Click	+= View_OnClearPointsButton_Click;
			_view.FigureList_IndexChanged	+= View_FigureList_IndexChanged;
			_view.WriteToFileButton_Click	+= View_WriteToFileButton_Click;
			_view.AddPointButton_Click		+= View_OnAddPointButton_Click;

			_view.CreateFigureButton_Click	+= View_OnCreateFigureButton_Click;
			_view.DeleteFigureButton_Click	+= View_OnDeleteFigureButton_Click;
			_view.EditPointButton_Click		+= View_EditPointButton_Click;
		}


		// ----------------------------------
		//		Methods-subscribers below
		// ----------------------------------


		private void View_FigureList_IndexChanged(object sender, int index)
		{
			if (index >= 0)
			{
				var points = (_figuresBuffer[index] as FigureDrawer).Adapter.GetRawPoints();

				object[] pts = new object[points.Length];

				points.CopyTo(pts, 0);

				_pointsBuffer.Clear();
				_pointsBuffer.AddRange(pts);

			}
		}

		private void View_EditPointButton_Click(object sender, int index, string newStringX, string newStringY)
		{
			if (index >= 0)
			{
				try
				{
					double x = Double.Parse(newStringX);
					double y = Double.Parse(newStringY);

					_pointsBuffer[index] = new Point(x, y);
				}
				catch (Exception err)
				{
					_view.ShowError(err);
				}
			}
			else
			{
				_view.ShowError(new Exception("Точка в списке не выделена."));
			}
		}

		private void View_OnReadFromFileButton_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.Filter = "Text files(*.txt)|*.txt";

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					var serializer = AppDependencies.CreateSerializer();

					try
					{
						var figures = serializer.Deserialize(dialog.FileName);

						_figuresBuffer.AddRange(figures.ToArray());
						_outFigures.AddRange(figures.ToArray());
					}
					catch (Exception err)
					{
						_view.ShowError(err);
					}
				}
			}
		}

		private void View_WriteToFileButton_Click(object sender, EventArgs e)
		{
			if (_figuresBuffer.Count == 0)
			{
				_view.ShowError(new Exception("Список фигур пуст."));
				return;
			}
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.Filter = "Text files(*.txt)|*.txt";

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					var serializer = AppDependencies.CreateSerializer();

					var list = new List<FigureDrawer>();

					foreach (var figure in _figuresBuffer)
					{
						list.Add(figure as FigureDrawer);
					}

					serializer.Serialize(list, dialog.FileName);
				}
			}
		}

		private void View_OnDeleteFigureButton_Click(object sender, int index)
		{
			if (index >= 0)
			{
				_figuresBuffer.RemoveAt(index);
				_outFigures.RemoveAt(index);
			}
			else
			{
				_view.ShowError(new Exception("Не выделена фигура в списке, не удалось удалить."));
			}
		}

		private void View_OnCreateFigureButton_Click(object sender, EventArgs e)
		{
			if (_pointsBuffer.Count == 0)
			{
				_view.ShowError(new Exception("Список точек пуст!"));
				return;
			}

			var factory = AppDependencies.CreataFactory();
			var points = new Point[_pointsBuffer.Count];

			for (int i = 0; i < points.Length; i++)
			{
				points[i] = (Point)_pointsBuffer[i];
			}

			try
			{
				var figure = factory.Create(points);
				var color = _view.GetSelectedColor();
				var figureDrawer = new FigureDrawer(
					new FiguresDataAdapter(figure),
					color,
					System.Drawing.Color.Black,
					System.Drawing.Color.Black
				);

				_figuresBuffer.Add(figureDrawer);
				_outFigures.Add(figureDrawer);
			}
			catch (Exception err)
			{
				_view.ShowError(err);
			}
		}

		private void View_OnClearPointsButton_Click(object sender, EventArgs e)
		{
			_pointsBuffer.Clear();
		}

		private void View_OnAddPointButton_Click(object sender, string stringX, string stringY)
		{
			try
			{
				double x = Double.Parse(stringX);
				double y = Double.Parse(stringY);

				_pointsBuffer.Add(new Point(x, y));
			}
			catch (Exception err)
			{
				_view.ShowError(err);
			}
		}

		public void ReceiveData(IPresenter sender, EventArgs args)
		{
			if (args is FigureDrawnEventArgs)
			{
				_outFigures = (args as FigureDrawnEventArgs).Figures;
				_figuresBuffer.AddRange(_outFigures);
			}
			else
			{
				throw new ArgumentException("invalid event args '" + nameof(args) + "' sended");
			}
		}
	}
}
