using System;

namespace Magus
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MagusTableAttribute : Attribute
    {
        public string TableName { get; }
        
        public MagusTableAttribute(string tableName)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class PrimaryKeyAttribute : Attribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class IndexAttribute : Attribute
    {
        public int Order { get; }
        public int CombinedOrder { get; }

        public IndexAttribute(int order, int combinedOrder = 0)
        {
            Order = order;
            CombinedOrder = combinedOrder;
        }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class MagusConstructorAttribute : Attribute
    {
    }
}