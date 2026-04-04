using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TemplateEdit;
public static class EnumHelper
{
    public static Array DataSourceTypes => Enum.GetValues(typeof(DataSourceType));
    public static Array DataSourceFieldTypes => Enum.GetValues(typeof(FieldTypes));
}

