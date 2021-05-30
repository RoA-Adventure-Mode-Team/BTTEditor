using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace RivalsAdventureEditor.Extensions
{
    public class NullContainerSelector : StyleSelector
    {
        public Style Default { get; set; }
        public Style Separator { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item == null)
                return Separator;
            return Default;
        }
    }
}
