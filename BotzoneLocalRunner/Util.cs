using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace BotzoneLocalRunner
{
	public class RangeObservableCollection<T> : ObservableCollection<T>
	{
		public RangeObservableCollection() : base() { }
		public RangeObservableCollection(List<T> list) : base(list) { }
		public RangeObservableCollection(IEnumerable<T> collection) : base(collection) { }

		private bool _suppressNotification = false;

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (!_suppressNotification)
				base.OnCollectionChanged(e);
		}

		public void AddRange(IEnumerable<T> list)
		{
			if (list == null)
				throw new ArgumentNullException("list");

			_suppressNotification = true;

			foreach (T item in list)
			{
				Add(item);
			}
			_suppressNotification = false;
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}

	public class ValueDescription
	{
		public Enum Value { get; set; }
		public string Description { get; set; }
		public static implicit operator Enum(ValueDescription me)
		{
			return me.Value;
		}
	}

	[ValueConversion(typeof(ValueDescription), typeof(Enum))]
	public class ValueDescriptionToEnumConverter : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return new ValueDescription { Value = (Enum)value, Description = (value as Enum).Description() };
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as ValueDescription)?.Value;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	[ValueConversion(typeof(Enum), typeof(IEnumerable<ValueDescription>))]
	public class EnumToCollectionConverter : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return EnumHelper.GetAllValuesAndDescriptions(value.GetType());
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	public static class EnumHelper
	{
		public static string Description(this Enum eValue)
		{
			var nAttributes = eValue.GetType().GetField(eValue.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (nAttributes.Any())
				return (nAttributes.First() as DescriptionAttribute).Description;

			return eValue.ToString();
		}

		public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t)
		{
			if (!t.IsEnum)
				throw new ArgumentException("t must be an enum type");

			return from Enum e in Enum.GetValues(t)
				   select new ValueDescription { Value = e, Description = e.Description() };
		}
	}

	internal static class Util
    {
		internal static ILogger Logger { get; set; }
    }
}
