using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

public static class AttributeUsageFinder
{ 
    public static Type[] GetUsages<TAttribute>() where TAttribute : Attribute
    {
        IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.IsDefined(typeof(TAttribute))));

        return types.ToArray();
    }
}