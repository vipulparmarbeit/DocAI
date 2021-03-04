using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DoctorAI.Converters
{
    public class BoolToParameterConditionalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string[] parameters = parameter.ToString().Split(':');
            string resultIfTrue = parameters[0];
            string resultIfFalse = parameters[1];
            return (bool)value ? resultIfTrue : resultIfFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
