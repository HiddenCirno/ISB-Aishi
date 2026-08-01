using EFT.InventoryLogic;
using System.Collections;
using System.Reflection;

namespace AishiKeys.MasterKey
{
    internal static class MasterKeyItemResolver
    {
        internal static KeyComponent FindKeyComponent(Item item)
        {
            if (item == null)
                return null;

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            for (System.Type current = item.GetType();
                 current != null;
                 current = current.BaseType)
            {
                FieldInfo field = current.GetField("Components", flags);
                if (field != null)
                {
                    KeyComponent component = FindInEnumerable(
                        field.GetValue(item) as IEnumerable);
                    if (component != null)
                        return component;
                }

                PropertyInfo property = current.GetProperty("Components", flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    KeyComponent component = FindInEnumerable(
                        property.GetValue(item, null) as IEnumerable);
                    if (component != null)
                        return component;
                }
            }

            return null;
        }

        private static KeyComponent FindInEnumerable(IEnumerable components)
        {
            if (components == null)
                return null;

            foreach (object component in components)
            {
                KeyComponent keyComponent = component as KeyComponent;
                if (keyComponent != null)
                    return keyComponent;
            }

            return null;
        }
    }
}
