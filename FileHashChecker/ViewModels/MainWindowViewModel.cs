﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using FileHashChecker.Models;

namespace FileHashChecker.ViewModels
{
	public class MainWindowViewModel : DependencyObject
	{
		#region Property

		public string SourceFilePath
		{
			get { return (string)GetValue(SourceFilePathProperty); }
			set { SetValue(SourceFilePathProperty, value); }
		}
		public static readonly DependencyProperty SourceFilePathProperty =
			DependencyProperty.Register(
				"SourceFilePath",
				typeof(string),
				typeof(MainWindowViewModel),
				new PropertyMetadata(null));

		public string CompareToTarget
		{
			get { return _compareToTarget; }
			set
			{
				_compareToTarget = value;
				CompareHash(value);
			}
		}
		private string _compareToTarget;

		public bool IsReading
		{
			get { return (bool)GetValue(IsReadingProperty); }
			set { SetValue(IsReadingProperty, value); }
		}
		public static readonly DependencyProperty IsReadingProperty =
			DependencyProperty.Register(
				"IsReading",
				typeof(bool),
				typeof(MainWindowViewModel),
				new PropertyMetadata(
					false,
					null,
					(d, e) => ((MainWindowViewModel)d).Hashes.Any(x => x.IsReading)));

		public double ProgressRate
		{
			get { return (double)GetValue(ProgressRateProperty); }
			set { SetValue(ProgressRateProperty, value); }
		}
		public static readonly DependencyProperty ProgressRateProperty =
			DependencyProperty.Register(
				"ProgressRate",
				typeof(double),
				typeof(MainWindowViewModel),
				new PropertyMetadata(
					0D,
					null,
					(d, e) => ((MainWindowViewModel)d).Hashes.FirstOrDefault(x => x.IsReading)?.ProgressRate ?? 0D));

		#endregion

		public IReadOnlyCollection<HashViewModel> Hashes { get; }

		public MainWindowViewModel()
		{
			Hashes = new[]
			{
				new HashViewModel(HashType.Sha1) { IsEnabled = true },
				new HashViewModel(HashType.Sha256),
				new HashViewModel(HashType.Sha512),
				new HashViewModel(HashType.Md5)
			};
		}

		public async Task CheckFileAsync(IEnumerable<string> filePaths)
		{
			var filePath = filePaths?.FirstOrDefault(x => File.Exists(x));
			if (filePath != null)
			{
				SourceFilePath = filePath;
				await GetHashAsync(filePath);
			}
		}

		private async Task GetHashAsync(string filePath)
		{
			foreach (var vm in Hashes)
				vm.ClearHash();

			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				foreach (var vm in Hashes.Where(x => x.IsEnabled))
				{
					SetBindings(vm); // Bindings will be overwritten and so only the last set ones remain.

					await vm.GetHashAsync(fs);
					vm.CompareHash(CompareToTarget);
				}
			}
		}

		private void SetBindings(HashViewModel vm)
		{
			BindingOperations.SetBinding(
				this,
				IsReadingProperty,
				new Binding(nameof(HashViewModel.IsReading))
				{
					Source = vm,
					Mode = BindingMode.OneWay
				});

			BindingOperations.SetBinding(
				this,
				ProgressRateProperty,
				new Binding(nameof(HashViewModel.ProgressRate))
				{
					Source = vm,
					Mode = BindingMode.OneWay
				});
		}

		private void ClearBindings()
		{
			BindingOperations.ClearBinding(
				this,
				IsReadingProperty);

			BindingOperations.ClearBinding(
				this,
				ProgressRateProperty);
		}

		private void CompareHash(string compareToTarget)
		{
			Parallel.ForEach(Hashes, vm => vm.CompareHash(compareToTarget));
		}
	}
}